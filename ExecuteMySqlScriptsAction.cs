using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Actions.Database;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.Database;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Inedo.BuildMasterExtensions.MySql
{
    
    [ActionProperties("Execute Database MySqlScripts on Disk", "Finds files matching a search mask (e.g. *.sql) and executes those scripts against a database connection with MySqlScript."), Tag("databases"), CustomEditor(typeof(ExecuteMySqlScriptsActionEditor))]
    public sealed class ExecuteMySqlScriptsAction : DatabaseBaseAction // DatabaseBaseMySqlAction
    {
        [Persistent]
        public string SearchPattern
        {
            get;
            set;
        }

        [Persistent]
        public bool WarnIfNoScripts
        {
            get;
            set;
        }

        [Persistent]
        public string Delimiter
        {
            get;
            set;
        }

        public ExecuteMySqlScriptsAction()
        {
            this.WarnIfNoScripts = true;
        }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(new ShortActionDescription(new object[]
			{
				"Execute ",
				new Hilite(this.SearchPattern),
				" Against ",
				new Hilite(this.GetProviderNameSafe())
			}), new LongActionDescription(new object[]
			{
				"from ",
				new DirectoryHilite(base.OverriddenSourceDirectory)
			}));
        }

        protected override void ExecuteProviderBasedAction()
        {
            base.LogDebug("Loading scripts to execute...");
            IFileOperationsExecuter service = base.Context.Agent.GetService<IFileOperationsExecuter>();
            DirectoryEntryInfo entry = service.GetDirectoryEntry(new GetDirectoryEntryCommand
            {
                Path = base.Context.SourceDirectory,
                IncludeRootPath = true,
                Recurse = true
            }).Entry;

            List<FileEntryInfo> list = (
                from f in Util.Files.Comparison.GetMatches(base.Context.SourceDirectory, entry, new string[]
				{
					Util.CoalesceStr<string, string>(this.SearchPattern, "*")
				}).OfType<FileEntryInfo>()
                orderby f.Path
                select f).ToList<FileEntryInfo>();

            if (list.Count == 0)
            {
                base.Log(this.WarnIfNoScripts ? MessageLevel.Warning : MessageLevel.Information, "No scripts were found to execute.");
                return;
            }

            base.LogInformation("Executing {0} script files...", new object[]
			{
				list.Count
			});

            foreach (FileEntryInfo current in list)
            {
                base.LogDebug("Executing {0}...", new object[]
				{
					current.Path.Substring(base.Context.SourceDirectory.Length).Trim(new char[]
					{
						service.GetDirectorySeparator()
					})
				});
                string query = service.ReadAllText(current.Path);

                var provider = base.Provider;
                ((MySqlDatabaseProvider)provider).ExecuteScript(query, this.Delimiter);
            }


            base.LogInformation("Script execution complete.");
        }
    }
}

using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Actions.Database;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.MySql
{
    internal sealed class ExecuteMySqlScriptsActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtSearchPattern;
        private ValidatingTextBox txtDelimiter;
        private CheckBox chkWarnIfNoScripts;
        public override string ServerLabel
        {
            get
            {
                return "On server:";
            }
        }

        public override string SourceDirectoryLabel
        {
            get
            {
                return "From directory:";
            }
        }

        public override bool DisplaySourceDirectory
        {
            get
            {
                return true;
            }
        }

        public ExecuteMySqlScriptsActionEditor()
        {
            base.ValidateBeforeSave += new EventHandler<ValidationEventArgs<ActionBase>>(this.ExecuteMySqlScriptsActionEditor_ValidateBeforeSave);
        }

        public override ActionBase CreateFromForm()
        {
            return new ExecuteMySqlScriptsAction
            {
                SearchPattern = this.txtSearchPattern.Text,
                Delimiter = this.txtDelimiter.Text,
                WarnIfNoScripts = this.chkWarnIfNoScripts.Checked,
            };
        }

        public override void BindToForm(ActionBase action)
        {
            ExecuteMySqlScriptsAction executeScriptsAction = (ExecuteMySqlScriptsAction)action;
            this.txtSearchPattern.Text = (executeScriptsAction.SearchPattern ?? string.Empty);
            this.txtDelimiter.Text = (executeScriptsAction.Delimiter ?? string.Empty);
            this.chkWarnIfNoScripts.Checked = executeScriptsAction.WarnIfNoScripts;
        }

        protected override void CreateChildControls()
        {
            this.txtSearchPattern = new ValidatingTextBox
            {
                Text = "*.sql"
            };
            this.txtDelimiter = new ValidatingTextBox
            {
                Text = ";"
            };
            this.chkWarnIfNoScripts = new CheckBox
            {
                Text = "Warn if no scripts are found to execute",
                Checked = true
            };
            this.Controls.Add(new Control[]
			{
				new SlimFormField("Search pattern:", this.txtSearchPattern),
				new SlimFormField("Delimiter:", this.txtDelimiter),
                new SlimFormField("Additional options:", this.chkWarnIfNoScripts),
			});
        }

        private void ExecuteMySqlScriptsActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            DatabaseBaseAction action = (DatabaseBaseAction)e.Extension;
            IEnumerable<Tables.DatabaseChangeScriptProviders_Extended> source = StoredProcs.DatabaseChangeScripts_GetDatabaseProviders(new int?(base.ApplicationId), null).Execute();
            Tables.DatabaseChangeScriptProviders_Extended databaseChangeScriptProviders_Extended = string.IsNullOrEmpty(action.ProviderName) ? StoredProcs.DatabaseChangeScripts_GetDatabaseProvider(new int?(action.ProviderId)).Execute() : source.FirstOrDefault((Tables.DatabaseChangeScriptProviders_Extended p) => string.Equals(p.Provider_Name, action.ProviderName, StringComparison.OrdinalIgnoreCase));
            if (databaseChangeScriptProviders_Extended != null)
            {
                // todo check selected provider is MySqlDatabaseProvider
                if (!databaseChangeScriptProviders_Extended.Provider_Configuration.Contains("MySqlDatabaseProvider"))
                {
                    e.Message = "The database connection with the specified name is not configured with the MySql provider.";
                    e.ValidLevel = ValidationLevel.Error;
                    return;
                }

                // todo Inedo.BuildMaster.Web.Security not includede in SDK
                //if (WebUserContext.CanPerformTask(SecuredTask.DatabaseChangeScripts_ViewDatabaseProvider, null, new int?(databaseChangeScriptProviders_Extended.Application_Id), new int?(databaseChangeScriptProviders_Extended.Environment_Id), null))
                //{
                //    return;
                //}
                //e.Message = "You do not have the DatabaseChangeScripts_ViewDatabaseProvider privilege for the specified database connection.";
                //e.ValidLevel = ValidationLevel.Error;
                //return;
            }
            else
            {
                // todo Inedo.BuildMaster.Web.Security not includede in SDK
                //if (source.Any((Tables.DatabaseChangeScriptProviders_Extended i) => !WebUserContext.CanPerformTask(SecuredTask.DatabaseChangeScripts_ViewDatabaseProvider, null, new int?(i.Application_Id), new int?(i.Environment_Id), null)))
                //{
                //    e.Message = "There is no database connection with the specified names, and you do not have DatabaseChangeScripts_ViewDatabaseProvider privileges for all database connections.";
                //    e.ValidLevel = ValidationLevel.Error;
                //    return;
                //}
                if (databaseChangeScriptProviders_Extended == null && !action.ProviderName.StartsWith("$") && !action.ProviderName.StartsWith("%"))
                {
                    e.Message = "There are no Database Connections named \"" + action.ProviderName + "\"; this may cause errors at execution time.";
                    e.ValidLevel = ValidationLevel.Warning;
                }
                return;
            }
        }
    }
}

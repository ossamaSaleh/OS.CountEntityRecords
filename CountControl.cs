using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;

namespace OS.CountEntityRecords
{
    public partial class CountControl : PluginControlBase
    {
        private Settings mySettings;
        DataTable dataTable = new DataTable();
        public CountControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        public void tsbSample_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(() => GetAllEntities());
        }
        public void GetAllEntities()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting Entities",
                Work = (worker, args) =>
                  {
                      args.Result = GetMetadata(Service);
                  },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result;
                    if (result != null)
                    {
                        
                        dgEntity.AutoGenerateColumns = false;
                        dgEntity.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                        dgEntity.ColumnCount = 1;
                        dgEntity.Columns[0].Name = "SchemaName";
                        dgEntity.Columns[0].HeaderText = "Schema Name";
                        dgEntity.Columns[0].DataPropertyName = "SchemaName";
                        dgEntity.DataSource = result;
                       
                    }
                }
            });

        }
        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account")
                    {
                        TopCount = 50
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        MessageBox.Show($"Found {result.Entities.Count} accounts");
                    }
                }
            });
        }
        public static EntityMetadata[] GetMetadata(IOrganizationService crmService)
        {
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Privileges
                
            };

            var response = (RetrieveAllEntitiesResponse)crmService.Execute(request);
            return response.EntityMetadata;
        }
        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }
        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;
        private void dgEntity_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgEntity.SelectedRows)
            {
                string EntityName = row.Cells[0].Value.ToString();
                //var r = new RetrieveTotalRecordCountRequest();
                //r.EntityNames = new[] { EntityName.ToLower() };
                //    var m = ((RetrieveTotalRecordCountResponse)Service.Execute(r)).EntityRecordCountCollection;

                //    foreach (var item in m.Values)
                //    {
                //        txtCount.Text = item.ToString();
                //    }
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Getting Counts",
                    Work = (worker, args) =>
                    {
                        args.Result = GetCountOfEntity(EntityName, Service);

                    },
                    ProgressChanged = (args) =>
                    {
                        // If progress has to be notified to user, use the following method:
                        SetWorkingMessage("Done");

                        // If progress has to be notified to user, through the
                        // status bar, use the following method
                        if (SendMessageToStatusBar != null)
                            SendMessageToStatusBar(this, new StatusBarMessageEventArgs(args.ProgressPercentage, args.UserState.ToString()));
                    },
                    PostWorkCallBack = (args) =>
                    {
                        if (args.Error != null)
                        {
                            MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        var result = args.Result;
                        if (result != null)
                        {
                            txtCount.Text = args.Result.ToString();
                        }
                    }
                });

            }
        }

        private long GetCountOfEntity(string entityName, IOrganizationService service)
        {
            long data = 0;
            var r = new RetrieveTotalRecordCountRequest();
            r.EntityNames = new[] {entityName.ToLower()};
            try
            {
                var m = ((RetrieveTotalRecordCountResponse)service.Execute(r)).EntityRecordCountCollection;

                foreach (var item in m.Values)
                {
                    data = item;
                }
            }
            catch (Exception ex) { data = 0; }
            return data;
        }

       
        private void btnSearch_Click(object sender, EventArgs e)
        {
            var searchValue = txtSearch.Text;

            dgEntity.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            try
            {
                foreach (DataGridViewRow row in dgEntity.Rows)
                {
                    if (row.Cells[0].Value.ToString().Equals(searchValue))
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }

        }
    }
}
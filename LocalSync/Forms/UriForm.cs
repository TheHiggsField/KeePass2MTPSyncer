using System;
using System.Collections.Generic;
using System.Windows.Forms;
using KeePass;
using KeePassLib;
using LocalSync.TransferClients;
using Newtonsoft.Json;

namespace LocalSync.Forms
{
    public partial class UriForm : Form
    {
        private Label lblPromptUri;
        private Label lblPromptUserId; 
        private TextBox tbxUri;
        private TextBox tbxUserId;
        private Button btnSave;
        private TableLayoutPanel layout;
        private IList<PwDatabase> pwDatabases;

        public string ConfigString { get; private set; } = null;
        private HttpTransferClientFactory transferClientFactory = new HttpTransferClientFactory();
        public ITransferClient TransferClient { get; private set; } = null;


        private const string transferClientTypeKey= "LocalSync.TransferClient.type";

        private string TransferClientType
        {
            get => Program.Config.CustomConfig.GetString(transferClientTypeKey, null);
            set  { Program.Config.CustomConfig.SetString(transferClientTypeKey, value); }
        }

        public UriForm(string ConfigString, IList<PwDatabase> _pwDatabases)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(ConfigString))
            {
                try
                {
                    var config = JsonConvert.DeserializeObject<HttpTransferClientConfig>(ConfigString);

                    tbxUri.Text = config.ServerEndpoint;
                    tbxUserId.Text = config.SharedKeyUserId;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to read stored config:\n" + ex.ToString());
                }
            }

            pwDatabases = _pwDatabases;

        }

        private void InitializeComponent()
        {

            // Label
            lblPromptUri = new Label()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Name = "lblPrompt",
                Text = "Server URI used to connect to you phone:"
            };

            // TextBox
            tbxUri = new TextBox()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Name = "tbxUri"
            };

            // Label
            lblPromptUserId = new Label()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Name = "lblPrompt",
                Text = "UserId to authenticate with:"
            };

            // TextBox
            tbxUserId = new TextBox()
            {
                Anchor = AnchorStyles.Left,
                Name = "tbxConfigName"
            };

            // SaveButton
            btnSave = new Button()
            {
                Anchor = AnchorStyles.Right,
                Name = "btnSave",
                Text = "Save"
            };
            btnSave.Click += new EventHandler(buttonOK_Click);


            layout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                RowCount = 5 // Create a Phantom row to take up extra vertical space
            };

            layout.Controls.Add(lblPromptUri, 0, 0);
            layout.Controls.Add(tbxUri, 0, 1);
            layout.Controls.Add(lblPromptUserId, 0, 2);
            layout.Controls.Add(tbxUserId, 0, 3);
            layout.Controls.Add(btnSave, 0, 4);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            Controls.Add(layout);

            // UriForm
            Padding = new Padding() { Left = 50, Right = 50 };
            MinimumSize = new System.Drawing.Size(600, 130);
            Name = "UriForm";
            Text = "Enter Endpoint";
        }

        private async void buttonOK_Click(object sender, EventArgs e)
        {
            // Button click event handler
            string uri = tbxUri.Text;
            string userId = tbxUserId.Text;


            transferClientFactory.Errors.Clear();
            var client = await transferClientFactory.ConfigureHttpTransferClient(uri, userId).SetSecrets(pwDatabases).Build();
            
            if (transferClientFactory.Errors.Count != 0)
            {
                ShowErrorsBox();
                return;
            }

            TransferClient = client;
            ConfigString = JsonConvert.SerializeObject(transferClientFactory.GetConfig());
            Close();
        }

        public void ShowErrorsBox()
        {
            MessageBox.Show(
                string.Join(
                    "\n------------------------------------------------------------------------------------\n",
                    transferClientFactory.Errors
                ),
                "Could not create client"
            );
        }
    }
}

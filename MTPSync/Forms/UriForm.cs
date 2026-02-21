using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KeePassLib;
using LocalSync.Extension;
using LocalSync.TransferClients;

namespace LocalSync.Forms
{
    public partial class UriForm : Form
    {
        private Label lblPrompt;
        private TextBox tbxUri;
        private TextBox tbxUserId;
        private Button btnSave;
        private TableLayoutPanel layout;
        private IList<PwDatabase> pwDatabases;

        public string ConfigString { get; private set; } = null;
        private TransferClientFactory transferClientFactory = new TransferClientFactory();
        public ITransferClient TransferClient { get; private set; } = null;

        public UriForm(string ConfigString, IList<PwDatabase> _pwDatabases)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(ConfigString))
                tbxUri.Text = ConfigString;

            pwDatabases = _pwDatabases;

        }

        private void InitializeComponent()
        {

            // Label
            lblPrompt = new Label()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Name = "lblPrompt",
                Text = "Enter the URI for the folder containing the databases on you phone:"
            };

            // TextBox
            tbxUri = new TextBox()
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                Name = "tbxUri"
            };

            // TextBox
            tbxUserId = new TextBox()
            {
                Anchor = AnchorStyles.Left,
                Name = "tbxconfigName"
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

            layout.Controls.Add(lblPrompt, 0, 0);
            layout.Controls.Add(tbxUri, 0, 1);
            layout.Controls.Add(tbxUserId, 0, 2);
            layout.Controls.Add(btnSave, 0, 3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
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
            string sharedKey = null;

            foreach (var db in pwDatabases)
            {
                sharedKey = db.GetSharedKey(userId);

                if (sharedKey != null)
                    break;

            }


            transferClientFactory.Errors.Clear();
            var client = await transferClientFactory.ConfigureHttpTransferClient(uri, userId, sharedKey).Build();

            if (transferClientFactory.Errors.Count != 0)
            {
                MessageBox.Show(string.Join("---------------------", transferClientFactory.Errors), "Could not create client");
                
                return;
            }

            TransferClient = client;
            ConfigString = uri;
            Close();
        }
    }
}

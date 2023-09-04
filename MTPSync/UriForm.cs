using System;
using System.IO;
using System.Windows.Forms;

namespace MTPSync
{
    public partial class UriForm : Form
    {
        private Label lblPrompt;
        private TextBox tbxUri;
        private Button btnSave;
        private TableLayoutPanel layout;

        private readonly IMTPClient mTPClient;

        public string UriResult { get; private set; } = null;

        public UriForm(string _currentUri, IMTPClient _mTPClient)
        {
            InitializeComponent();

            tbxUri.Text = _currentUri;
            mTPClient = _mTPClient;
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
                Anchor = AnchorStyles.Right| AnchorStyles.Left,
                Name = "tbxUri"
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
                RowCount = 4 // Create a Phantom row to take up extra vertical space
            };

            layout.Controls.Add(lblPrompt, 0, 0);
            layout.Controls.Add(tbxUri, 0, 1);
            layout.Controls.Add(btnSave, 0, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            Controls.Add(layout);

            // UriForm
            Padding = new Padding() { Left = 50, Right = 50 };
            MinimumSize = new System.Drawing.Size(600, 130);
            Name = "UriForm";
            Text = "Enter MTP URI";
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // Button click event handler
            string uri = tbxUri.Text;

            if (mTPClient.IsFolder(uri) != true)
            {
                MessageBox.Show("The path/URI was not found, or is not a Directory.", "Path/URI not found");
                return;
            }

            if (!uri.EndsWith(Path.DirectorySeparatorChar.ToString()))
                uri += Path.DirectorySeparatorChar;

            UriResult = uri;

            Close();
        }
    }
}

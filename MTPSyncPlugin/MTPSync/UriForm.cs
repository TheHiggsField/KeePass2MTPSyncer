using System;
using System.Windows.Forms;

namespace MTPSync
{
    public partial class UriForm : Form
    {
        private Label label1;
        private TextBox textBox1;
        private Button buttonOK;

        private Action<string> callBack;

        public UriForm(Action<string> _callBack)
        {
            InitializeComponent();

            callBack = _callBack;
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // Label
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 30);
            this.label1.Name = "label1";
            this.label1.Text = "Enter the URI for the folder containing the databases on you phone:";
            this.Controls.Add(this.label1);

            // TextBox
            this.textBox1.Location = new System.Drawing.Point(30, 55);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(440, 20);
            this.Controls.Add(this.textBox1);

            // OK Button
            this.buttonOK.Location = new System.Drawing.Point(150, 80);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Text = "Save";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            this.Controls.Add(this.buttonOK);

            // MyForm
            this.ClientSize = new System.Drawing.Size(500, 150);
            this.Name = "MyForm";
            this.Text = "Enter MTP URI";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // Button click event handler
            string uri = textBox1.Text;
            callBack(uri);
            this.Close();
        }
    }
}

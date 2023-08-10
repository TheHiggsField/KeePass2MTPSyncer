using System;
using System.IO;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using static System.Windows.Forms.LinkLabel;

namespace MTPSync
{
	public sealed class MTPSyncExt : Plugin
	{
		private IPluginHost m_host = null;
        private MTPSyncer syncer;
        
        string mtpSourceFolderKey = "MTPSync.MtpDevice.DatabaseFolder";

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        private string mtpSourceFolder
        {
            get => Program.Config.CustomConfig.GetString(mtpSourceFolderKey, string.Empty);

            set
            {
                Program.Config.CustomConfig.SetString(mtpSourceFolderKey, value);
            }
        }

        public override bool Initialize(IPluginHost host)
		{
			if(host == null) return false;
			m_host = host;

            syncer = new MTPSyncer(mtpSourceFolder, host.MainWindow);

            return true;
		}

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Provide a menu item for the main location(s)
            if(t == PluginMenuType.Main)
            {
                ToolStripMenuItem mainItem = new ToolStripMenuItem("Sync Databases from Phone");
                mainItem.Click += this.OnSyncDBsClicked;

                var uriItem = new ToolStripMenuItem("Update MTP device URI");
                uriItem.Click += this.ShowUriForm;

                var consoleItem = new ToolStripMenuItem("Where's my terminal Windows!");
                consoleItem.Click += this.WindowsIsDumbSoGetMeATerminal;


                mainItem.DropDownItems.AddRange
                (
                    new ToolStripDropDownItem[]
                    {
                        uriItem,
                        consoleItem
                    }
                );

                return mainItem;
            }

            return null; // No menu items in other locations
        }

        private void OnSyncDBsClicked(object sender, EventArgs e)
        {
            try 
            {
                syncer.SyncDatabases();
            }
            catch (InvalidOperationException)
            {
                ShowUriForm(sender, e);

                try 
                {
                    syncer.SyncDatabases();
                }
                catch (InvalidOperationException)
                {
                    // Dialog was closed - abandon sync. 
                }
            }
        }

        private void ShowUriForm(object sender, EventArgs e)
        {
            UriForm uriForm = new UriForm(UdateMtpSyncerPath, mtpSourceFolder);

            uriForm.ShowDialog();

        }

        private void UdateMtpSyncerPath(string path)
        {
            mtpSourceFolder = path;
            syncer.SetMtpPath(path);
        }

        private void WindowsIsDumbSoGetMeATerminal(object sender, EventArgs e)
        {
            AllocConsole();
        }


	}
    
}
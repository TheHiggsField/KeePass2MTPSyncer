using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using KeePass.App.Configuration;

namespace MTPSync
{
	public sealed class MTPSyncExt : Plugin
	{
		private IPluginHost m_host = null;
        private MTPSyncer syncer = null;
        
        string mtpSourceFolderKey = "MTPSync.MtpDevice.DatabaseFolder";

        public MTPSyncExt()
        {
            syncer = new MTPSyncer(string.Empty, null);
            
        }

		public override bool Initialize(IPluginHost host)
		{
			if(host == null) return false;
			m_host = host;
            
            UdateMtpSyncerPath(Program.Config.CustomConfig.GetString(mtpSourceFolderKey, string.Empty));

			return true;
		}

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Provide a menu item for the main location(s)
            if(t == PluginMenuType.Main)
            {
                ToolStripMenuItem tsmi = new ToolStripMenuItem();
                tsmi.Text = "Sync Databases from Phone";
                tsmi.Click += this.OnSyncDBsClicked;
                return tsmi;
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
                UriForm uriForm = new UriForm(UdateMtpSyncerPath);

                uriForm.ShowDialog();

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

        private void UdateMtpSyncerPath(string path)
        {
            Program.Config.CustomConfig.SetString(mtpSourceFolderKey, path);

            syncer = new MTPSyncer(path, m_host.MainWindow);
        }

	}
    
}
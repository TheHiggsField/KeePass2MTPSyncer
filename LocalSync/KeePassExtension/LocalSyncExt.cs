using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using LocalSync.Forms;

namespace LocalSync
{
	public sealed class LocalSyncExt : Plugin
	{
		private IPluginHost m_host = null;
        private LocalSyncer syncer;
        
        string localSyncServerConfigKey = "LocalSync.Server.ServerConfig";

        public override string UpdateUrl => "https://raw.githubusercontent.com/TheHiggsField/KeePass2MTPSyncer/Windows/LocalSync/VersionInfo.txt";

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        private string LocalSyncServerConfig
        {
            get => Program.Config.CustomConfig.GetString(localSyncServerConfigKey, string.Empty);
            set { Program.Config.CustomConfig.SetString(localSyncServerConfigKey, value); }
        }

        public override bool Initialize(IPluginHost host)
		{
			if(host == null) return false;

			m_host = host;

            m_host.MainWindow.FileOpened += LocalSyncer.OpenFileHandler;
            return true;
		}

        public override void Terminate()
        {
            m_host.MainWindow.FileOpened -= LocalSyncer.OpenFileHandler;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Provide a menu item for the main location(s)
            if(t == PluginMenuType.Main)
            {
                ToolStripMenuItem mainItem = new ToolStripMenuItem("Sync Databases from Phone");
                mainItem.Click += OnSyncDBsClicked;

                var uriItem = new ToolStripMenuItem("Update LocalSync Server URI");
                uriItem.Click += ShowUriForm;
                mainItem.DropDownItems.Add(uriItem);

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var consoleItem = new ToolStripMenuItem("Where's my terminal Windows!");
                    consoleItem.Click += WindowsIsDumbSoGetMeATerminal;
                    mainItem.DropDownItems.Add(consoleItem);
                }

                return mainItem;
            }

            return null; // No menu items in other locations
        }

        private async void OnSyncDBsClicked(object sender, EventArgs e)
        {
            if (syncer == null)
            {
                ShowUriForm(sender, e, OnSyncDBsClicked);
                return;
            }

            await syncer.SyncDatabases();
        }

        private void ShowUriForm(object sender, EventArgs e) => ShowUriForm(sender, e, null);

        private void ShowUriForm(object sender, EventArgs e, Action<object, EventArgs> callBack)
        {
            var openDBs = m_host.MainWindow.DocumentManager.GetOpenDatabases();

            using (UriForm uriForm = new UriForm(LocalSyncServerConfig, openDBs))
            {
                uriForm.ShowDialog();

                if (uriForm.TransferClient == null)
                    return;

                syncer = new LocalSyncer(m_host.MainWindow, uriForm.TransferClient);
                LocalSyncServerConfig = uriForm.ConfigString;
            }

            callBack?.Invoke(sender, e);

        }

        private void WindowsIsDumbSoGetMeATerminal(object sender, EventArgs e)
        {
            AllocConsole();
        }
	}
}
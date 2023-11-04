using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;

namespace MTPSync
{
	public sealed class MTPSyncExt : Plugin
	{
		private IPluginHost m_host = null;
        private MTPSyncer syncer;
        
        string mtpSourceFolderKey = "MTPSync.MtpDevice.DatabaseFolder";

        public override string UpdateUrl => "https://raw.githubusercontent.com/TheHiggsField/KeePass2MTPSyncer/Windows/MTPSync/VersionInfo.txt";

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

            syncer = new MTPSyncer(host.MainWindow, mtpSourceFolder);

            m_host.MainWindow.FileOpened += syncer.OpenFileHandler;

            return true;
		}

        public override void Terminate()
        {
            m_host.MainWindow.FileOpened -= syncer.OpenFileHandler;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Provide a menu item for the main location(s)
            if(t == PluginMenuType.Main)
            {
                ToolStripMenuItem mainItem = new ToolStripMenuItem("Sync Databases from Phone");
                mainItem.Click += OnSyncDBsClicked;

                var uriItem = new ToolStripMenuItem("Update MTP device URI");
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

        private void OnSyncDBsClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(mtpSourceFolder))
            {
                ShowUriForm(sender, e, OnSyncDBsClicked);
                return;
            }

            syncer.SyncDatabases(mtpSourceFolder);
        }

        private void ShowUriForm(object sender, EventArgs e)
        {
            ShowUriForm(sender, e, null);
        }

        private void ShowUriForm(object sender, EventArgs e, Action<object, EventArgs> callBack)
        {
            UriForm uriForm = new UriForm(mtpSourceFolder, syncer.mtpClient);

            uriForm.ShowDialog();

            bool success = !string.IsNullOrEmpty(uriForm.UriResult);

            if (success)
                mtpSourceFolder = uriForm.UriResult;

            uriForm.Dispose();

            if (callBack != null && success)
                callBack(sender, e);
        }

        private void WindowsIsDumbSoGetMeATerminal(object sender, EventArgs e)
        {
            AllocConsole();
        }
	}
}
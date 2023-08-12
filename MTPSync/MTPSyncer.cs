using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gio;
using KeePass.DataExchange;
using KeePass.Forms;
using KeePass.Resources;

using KeePassLib;
using KeePassLib.Serialization;

namespace MTPSync
{

    public class MTPSyncer
    {
        private MTPAccess mtpClient = null;

        private MainForm mainWindow = null;
        
        string tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/KeePass/TempDBs/";

        public string mtpSourceFolder;

        public MTPSyncer(string _mtpSourceFolder, MainForm _mainForm)
        {

            mtpSourceFolder = _mtpSourceFolder;

            mainWindow = _mainForm;

            Directory.CreateDirectory(tempFolder);
        }

        public bool SyncDatabases()
		{
            if (string.IsNullOrEmpty(mtpSourceFolder))
                throw new InvalidOperationException("mtpSourceFolder cannot be null or empty.");

            if (mtpClient?.IsConnected != true)
            {
                mtpClient = new GioClient(mtpSourceFolder);

                if (!mtpClient.IsConnected)
                {
                    mainWindow.SetStatusEx("Mtp device is not connected!");
                    return false;
                }
            }

            bool AllSynced = CopyDBsToTemp(out var successfullyCopied);
            
            var openPwDBs = mainWindow.DocumentManager.GetOpenDatabases();

            int successfullySynced = 0;

            foreach( var pwDB in openPwDBs)
            {
                string filename = Path.GetFileName(pwDB.IOConnectionInfo.Path);

                bool wasCopiedFromPhone = successfullyCopied.Contains(filename);

                // Even if not copied the a version in temp might not have been synced yet
                // if the Master password wasn't available, when it was copied.
                bool wasSynced = SyncLocalDatabaseFiles(pwDB, tempFolder + filename);

                bool wasCopiedToPhone = false;

                if (wasCopiedFromPhone && wasSynced)
                {
                    wasCopiedToPhone = mtpClient.Upload(tempFolder + filename, mtpSourceFolder + filename);
                }

                Console.WriteLine("Syncing: " + filename + $"\t\tPhone->PC {boolToMessage(wasCopiedFromPhone && wasSynced)}\tPC->Phone {boolToMessage(wasCopiedToPhone)}");
                
                successfullySynced += wasCopiedFromPhone && wasSynced && wasCopiedToPhone ? 1 : 0;

                AllSynced = AllSynced && wasCopiedFromPhone && wasSynced && wasCopiedToPhone;
            }
			
        

            UpdateUISyncPost(AllSynced, successfullySynced, openPwDBs.Count, successfullyCopied.Count);
            
            return AllSynced;
		}

        public bool CopyDBsToTemp(out List<string> successfullyCopied)
        {            
            successfullyCopied = new List<string>();

            if (string.IsNullOrEmpty(mtpSourceFolder))
                return false;

            var DBNames = mtpClient.List(mtpSourceFolder).Where(fn => fn.EndsWith(".kdbx")).ToList();

            bool success = true;
            foreach (var filename in DBNames)
            {
                bool wasCoppied = mtpClient.Download(mtpSourceFolder + filename, tempFolder + filename);

                if (wasCoppied)
                    successfullyCopied.Add(filename);

                success = wasCoppied && success;
            }
            
            return success;
        }


        private bool SyncLocalDatabaseFiles(PwDatabase pwDB, string filePath)
		{         
            
			IOConnectionInfo ioc = IOConnectionInfo.FromPath
            (
                filePath
            );

			if(ioc == null || !ioc.CanProbablyAccess()) return false;

			MainForm mf = mainWindow;
			if((pwDB == null) || !pwDB.IsOpen) return false;

			bool? ob = ImportUtil.Synchronize(pwDB, mf, ioc, false, mf);

            // Remove the temp file from most recently used.
            if(ob == true)
                mainWindow.FileMruList.RemoveItem(ioc.GetDisplayName());

            return ob ?? false;
		}

        internal void UpdateUISyncPost(bool? obResult, int countSyncedDBs, int numOpenDBs, int numCoppiedDBs)
		{
			if(!obResult.HasValue) return;

			mainWindow.UpdateUI(false, null, true, null, true, null, false);
			mainWindow.SetStatusEx((obResult.Value ? KPRes.SyncSuccess : KPRes.SyncFailed) + $" ({countSyncedDBs}/{numOpenDBs} synced, {numCoppiedDBs} transfered.)");
		}

        private string boolToMessage(bool? success)
        {
            return success ?? false ? "succeeded" : "failed   ";
        }

        private void TestingStuff()
        {
            Console.WriteLine("\nFileMruList");
            Console.WriteLine(mainWindow.FileMruList);

            Console.WriteLine("\nGetOpenDatabases:");

            var openDBs = mainWindow.DocumentManager.GetOpenDatabases();
            Console.WriteLine(string.Join(", ", openDBs.Select(db => db.Name)));
            
            Console.WriteLine("\nDB ioc:");

            var connection = openDBs[0].IOConnectionInfo.Path;
            Console.WriteLine(connection);
            IOConnectionInfo connectionInfo = IOConnectionInfo.FromPath(tempFolder + "xxx.kdbx");
            Console.WriteLine(connectionInfo);
        }

        internal void SetMtpPath(string path)
        {
            mtpSourceFolder = path;
        }
    }
}
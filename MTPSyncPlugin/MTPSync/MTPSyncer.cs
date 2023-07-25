using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using KeePass.DataExchange;
using KeePass.Forms;
using KeePass.Resources;

using KeePassLib;
using KeePassLib.Serialization;

using Gio;

namespace MTPSync
{

    class MTPSyncer
    {
        private GioClient gioClient = null;

        private MainForm mainWindow = null;
        
        string tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/KeePass/TempDBs/";

        string mtpSourceFolder = null;

        public MTPSyncer(string mtpfolderUri, MainForm _mainForm)
        {
            
            gioClient = new GioClient();
            mtpSourceFolder = mtpfolderUri;

            mainWindow = _mainForm;

            Directory.CreateDirectory(tempFolder);
        }

        public bool SyncDatabases()
		{
            if (string.IsNullOrEmpty(mtpSourceFolder))
                throw new InvalidOperationException("mtpSourceFolder cannot be null or empty.");

            bool AllSynced = CopyDBsToTemp(out var sucessfullyCoppied);
            
            var openPwDBs = mainWindow.DocumentManager.GetOpenDatabases();

            int sucessfullySynced = 0;

            foreach( var pwDB in openPwDBs)
            {
                string filename = Path.GetFileName(pwDB.IOConnectionInfo.Path);

                bool wasCoppiedFromPhone = sucessfullyCoppied.Contains(filename);

                // Even if not copied the a version in temp might not have been synced yet if the Master password wasn't available, when it was coppied.
                bool wasSynced = SyncLocalDatabaseFiles(pwDB, tempFolder + filename);

                bool wasCoppiedToPhone = false;

                if (wasCoppiedFromPhone && wasSynced)
                {
                    wasCoppiedToPhone = gioClient.Copy(tempFolder + filename, mtpSourceFolder + filename);
                }

                Console.WriteLine("Syncing: " + filename + $"\t\tPhone->PC {boolToMessage(wasCoppiedFromPhone && wasSynced)}\tPC->Phone {boolToMessage(wasCoppiedToPhone)}");
                
                sucessfullySynced += wasCoppiedFromPhone && wasSynced && wasCoppiedToPhone ? 1 : 0;

                AllSynced = AllSynced && wasCoppiedFromPhone && wasSynced && wasCoppiedToPhone;
            }
			
        

            UpdateUISyncPost(AllSynced, sucessfullySynced, openPwDBs.Count, sucessfullyCoppied.Count);
            
            return AllSynced;
		}

        public bool CopyDBsToTemp(out List<string> sucessfullyCoppied)
        {            
            sucessfullyCoppied = new List<string>();

            if (string.IsNullOrEmpty(mtpSourceFolder))
                return false;

            var DBNames = gioClient.List(mtpSourceFolder).Where(fn => fn.EndsWith(".kdbx")).ToList();

            bool success = true;
            foreach (var filename in DBNames)
            {
                bool wasCoppied = gioClient.Copy(mtpSourceFolder + filename, tempFolder + filename);

                if (wasCoppied)
                    sucessfullyCoppied.Add(filename);

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
    }
}
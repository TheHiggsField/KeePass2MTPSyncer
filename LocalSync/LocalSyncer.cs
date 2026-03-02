using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KeePass.DataExchange;
using KeePass.Forms;
using KeePass.Resources;

using KeePassLib;
using KeePassLib.Serialization;

using LocalSync.Extension;
using LocalSync.TransferClients;

namespace LocalSync
{
    public class LocalSyncer
    {
        private readonly MainForm mainWindow = null;

        private readonly ITransferClient transferClient;

        public LocalSyncer(MainForm _mainForm, ITransferClient _transferClient)
        {
            mainWindow = _mainForm;
            transferClient = _transferClient;
        }

        public async Task<bool> SyncDatabases()
        {

            if (true != await transferClient?.IsConnected())
            {
                mainWindow.SetStatusEx("Server not found!");
                return false;
            }

            var copiedFileNames = await CopyDBsToTemp();

            var dBsFromTemp = GetDBsFromTemp();

            var openPwDBs = mainWindow.DocumentManager.GetOpenDatabases();

            int syncedCount = 0;
            bool AllSynced = true;

            foreach (var pwDB in openPwDBs)
            {

                bool dbFound = dBsFromTemp.TryGetValue(pwDB.GetDatabasePublicGuid(), out var tempFileName);

                if (!dbFound)
                    continue;

                // Even if not copied the version in temp might not have been synced yet
                // if the Master password wasn't available, when it was copied.
                bool wasSynced = SyncLocalDatabaseFiles(pwDB, tempFileName) && copiedFileNames.Contains(tempFileName);

                bool wasCopiedToPhone = false;

                if (wasSynced)
                {
                    wasCopiedToPhone = await transferClient.Upload(
                        tempFileName,
                        tempFileName
                    );
                }

                Console.WriteLine("Syncing: " + tempFileName + $"\t\tPhone->PC {boolToMessage(wasSynced)}\tPC->Phone {boolToMessage(wasCopiedToPhone)}");

                syncedCount += wasCopiedToPhone ? 1 : 0;

                AllSynced = AllSynced && wasCopiedToPhone;
            }


            UpdateUISyncPost(AllSynced, syncedCount, openPwDBs.Count, copiedFileNames.Count);

            return AllSynced;
        }

        public async Task<List<string>> CopyDBsToTemp()
        {
            var downloadedDBFiles = new List<string>();


            var listResult = await transferClient.List(null);
            var DBNames = listResult.Where(fn => Path.GetExtension(fn) == ".kdbx").ToList();

            bool success = true;
            foreach (var filename in DBNames)
            {
                var wasDownloaded = await transferClient.Download(
                    filename,
                    filename
                );
                
                success = wasDownloaded && success;

                if(wasDownloaded)
                {
                    downloadedDBFiles.Add(filename);
                }
            }
            
            return success ? downloadedDBFiles : null;
        }

        private Dictionary<Guid, string> GetDBsFromTemp()
        {
            bool success = true;
            var dBsInTemp = new Dictionary<Guid, string>();

            foreach (var filePath in Directory.GetFiles(transferClient.LocalStoreUri))
            {
                var pwDb = PwDatabase.LoadHeader(IOConnectionInfo.FromPath(filePath));

                Guid guid = pwDb?.ReadDatabasePublicGuid() ?? default;

                success &= UpdateDbFileNameDictionary(dBsInTemp, guid, filePath);
            }

            return success ? dBsInTemp : null;
        }

        private bool UpdateDbFileNameDictionary(Dictionary<Guid, string> dbDict, Guid dbGuid, string dbFilePath)
        {
            if (dbGuid == default)
                return false;

            if (dbDict.ContainsKey(dbGuid) )
            {
                bool shouldReplaceFileName = File.GetLastWriteTime(dbFilePath) > File.GetLastWriteTime(dbDict[dbGuid]);

                dbFilePath = shouldReplaceFileName ? dbFilePath : dbDict[dbGuid];
            }

            dbDict[dbGuid] = Path.GetFileName(dbFilePath);

            return true;
        }

        private bool SyncLocalDatabaseFiles(PwDatabase pwDB, string filePath)
        {
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(Path.Combine(transferClient.LocalStoreUri, filePath));

            bool? ob = null;

            if (ioc?.CanProbablyAccess() == true && pwDB?.IsOpen == true)
            {
                ob =  ImportUtil.Synchronize(pwDB, mainWindow, ioc, false, mainWindow);

                // Remove the temp file from most recently used.
                if (ob == true)
                    mainWindow.FileMruList.RemoveItem(ioc.GetDisplayName());
            }

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

        internal static void OpenFileHandler(object sender, FileOpenedEventArgs e)
        {
            Console.WriteLine($"DatabasePublicGuid ({e.Database.Name}):\n{e.Database.ReadDatabasePublicGuid()}");
            e.Database.SetDatabasePublicGuid();
        }

    }
}
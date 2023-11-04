using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using KeePass.DataExchange;
using KeePass.Forms;
using KeePass.Resources;

using KeePassLib;
using KeePassLib.Serialization;

namespace MTPSync
{

    public class MTPSyncer
    {
        private readonly MainForm mainWindow = null;
        
        private readonly string tempFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "KeePass", 
            "TempDBs"
        );

        public IMTPClient mtpClient {  get; private set; }

        public MTPSyncer(MainForm _mainForm, string mtpFolder)
        {
            mainWindow = _mainForm;

            mtpClient = GetMTPClient(mtpFolder);

            Directory.CreateDirectory(tempFolder);
        }

        public bool SyncDatabases(string mtpSourceFolder)
        {

            if (mtpClient?.IsConnected != true)
            {
                mtpClient?.IsFolder(mtpSourceFolder);

                if (mtpClient?.IsConnected != true)
                {
                    mainWindow.SetStatusEx("Mtp device is not found!");
                    return false;
                }
            }

            CopyDBsToTemp(mtpClient, mtpSourceFolder, out var copiedFileNames);

            GetDBsFromTemp(out var dBsFromTemp);

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
                bool wasSynced = SyncLocalDatabaseFiles(pwDB, Path.Combine(tempFolder, tempFileName)) && copiedFileNames.Contains(tempFileName);

                bool wasCopiedToPhone = false;

                if (wasSynced)
                {
                    wasCopiedToPhone = mtpClient.Upload(
                        Path.Combine(tempFolder, tempFileName), 
                        Path.Combine(mtpSourceFolder, tempFileName)
                    );
                }

                Console.WriteLine("Syncing: " + tempFileName + $"\t\tPhone->PC {boolToMessage(wasSynced)}\tPC->Phone {boolToMessage(wasCopiedToPhone)}");

                syncedCount += wasCopiedToPhone ? 1 : 0;

                AllSynced = AllSynced && wasCopiedToPhone;
            }


            UpdateUISyncPost(AllSynced, syncedCount, openPwDBs.Count, copiedFileNames.Count);

            return AllSynced;
        }

        public bool CopyDBsToTemp(IMTPClient mtpClient, string mtpSourceFolder, out List<string> downloadedDBFiles)
        {
            downloadedDBFiles = new List<string>();

            if (string.IsNullOrEmpty(mtpSourceFolder))
                return false;

            var DBNames = mtpClient.List(mtpSourceFolder).Where(fn => Path.GetExtension(fn) == ".kdbx").ToList();

            bool success = true;
            foreach (var filename in DBNames)
            {
                var wasDownloaded = mtpClient.Download(
                    Path.Combine(mtpSourceFolder, filename),
                    Path.Combine(tempFolder, filename)
                );
                
                success = wasDownloaded && success;

                if(wasDownloaded)
                {
                    downloadedDBFiles.Add(filename);
                }
            }
            
            return success;
        }

        private bool GetDBsFromTemp(out Dictionary<Guid, string> dBsInTemp)
        {
            bool success = true;
            dBsInTemp = new Dictionary<Guid, string>();

            foreach (var filePath in Directory.GetFiles(tempFolder))
            {
                var pwDb = PwDatabase.LoadHeader(IOConnectionInfo.FromPath(filePath));

                Guid guid = pwDb?.ReadDatabasePublicGuid() ?? default;

                success &= UpdateDbFileNameDictionary(dBsInTemp, guid, filePath);
            }

            return success;
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
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(filePath);

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

        internal void OpenFileHandler(object sender, FileOpenedEventArgs e)
        {
            Console.WriteLine($"DatabasePublicGuid ({e.Database.Name}):\n{e.Database.ReadDatabasePublicGuid()}");
            e.Database.SetDatabasePublicGuid();
        }

        public static IMTPClient GetMTPClient(string path)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return new GioClient(path);
                case PlatformID.Win32NT:
                    return new MediaDeviceClient(path);
                default:
                {
                    Debug.Assert(false, "No MTP client found for you OS");
                    return null;
                }
            }
        }

    }
}
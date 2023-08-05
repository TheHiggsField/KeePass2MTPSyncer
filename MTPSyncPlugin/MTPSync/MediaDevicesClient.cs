using System;
using System.Collections.Generic;
using System.Linq;
using MediaDevices;
using System.IO;

namespace MTPSync
{
    public class MediaDeviceClient : MTPAccess
    {
        private MediaDevice device;

        public MediaDeviceClient()
        {
            InitializeMtpDevice("Xperia XZ1 Compact");
        }
        public MediaDeviceClient(string friendlyName)
        {
            InitializeMtpDevice(friendlyName);
        }

        private void InitializeMtpDevice(string friendlyName)
        {
            // Find the first connected MTP device
            var devices = MediaDevice.GetDevices().ToList();

            device = devices?.FirstOrDefault(d => d.FriendlyName == friendlyName);

            if (device != null)
            {
                device.Connect();
            }
            else
            {
                throw new InvalidOperationException("MTP device not found.");
            }
        }

        public bool Download(string sourcePath, string destinationPath)
        {
            try
            {
                using (FileStream stream = File.Create(destinationPath))
                {
                    device.DownloadFile(sourcePath, stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy error: {ex.Message}");
                return false;
            }
        }

        public bool Upload(string sourcePath, string mtpPath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(sourcePath))
                {
                    if(device.FileExists(mtpPath))
                    {
                        device.DeleteFile(mtpPath);
                    }
                    device.UploadFile(sourcePath, mtpPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy error: {ex.Message}");
                return false;
            }
        }

        public List<string> List(string mtpSourcePath)
        {
            try
            {
                // Get the list of files/folders in the specified MTP directory
                return device.EnumerateFiles(mtpSourcePath).Select(path => Path.GetFileName(path)).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"List error: {ex.Message}");
                return new List<string>();
            }
        }
    }

}

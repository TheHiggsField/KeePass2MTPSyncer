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
            InitializeMtpDevice();
        }

        private void InitializeMtpDevice()
        {
            // Find the first connected MTP device
            var devices = MediaDevice.GetDevices().ToList();
            if (devices.Count > 0)
            {
                device = devices[0];
                device.Connect();
            }
            else
            {
                throw new InvalidOperationException("No MTP device found.");
            }
        }

        public bool Copy(string sourcePath, string destinationPath)
        {
            try
            {
                using (FileStream stream = File.OpenWrite(destinationPath))
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

        public List<string> List(string mtpSourcePath)
        {
            try
            {
                // Get the list of files/folders in the specified MTP directory
                var files = device.EnumerateFiles(mtpSourcePath);
                return files.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"List error: {ex.Message}");
                return new List<string>();
            }
        }
    }

}

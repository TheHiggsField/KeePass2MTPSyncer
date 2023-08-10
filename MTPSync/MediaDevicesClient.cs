using System;
using System.Collections.Generic;
using System.Linq;
using MediaDevices;
using System.IO;
using System.Security.Policy;

namespace MTPSync
{
    public class MediaDeviceClient : MTPAccess
    {
        private MediaDevice device;

        private bool initialized = false;

        public MediaDeviceClient(string mtpPath)
        {
            RelativePath(mtpPath, out var deviceName);
            InitializeMtpDevice(deviceName);
        }

        private void InitializeMtpDevice(string friendlyName)
        {
            if (!string.IsNullOrEmpty(friendlyName) && !IsConnected)
            {
                // Find the first connected MTP device
                var devices = MediaDevice.GetDevices().ToList();

                device = devices?.FirstOrDefault(d => d.FriendlyName == friendlyName);

                if (device != null)
                {
                    device.Connect();
                    device.DeviceCapabilitiesUpdated += (s, e) => { initialized = false; };
                    device.DeviceRemoved += (s, e) => { initialized = false; };
                    initialized = true;
                }
            }
        }

        public bool Download(string mtpPath, string destinationPath)
        {
            mtpPath = RelativePath(mtpPath, out var deviceName);
            InitializeMtpDevice(deviceName);

            try
            {
                if (device.FileExists(mtpPath))
                {
                    using (FileStream stream = File.Create(destinationPath))
                    {

                        device.DownloadFile(mtpPath, stream);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
                return false;
            }
        }

        public bool Upload(string sourcePath, string mtpPath)
        {
            mtpPath = RelativePath(mtpPath, out var deviceName);
            InitializeMtpDevice(deviceName);
            string tempFilePath = null;

            try
            {
                using (FileStream stream = File.OpenRead(sourcePath))
                {                    
                    if (device.FileExists(mtpPath))
                    {
                        tempFilePath = mtpPath + Guid.NewGuid().ToString();
                        device.Rename(mtpPath, tempFilePath);
                    }

                    device.UploadFile(sourcePath, mtpPath);

                    device.DeleteFile(tempFilePath);

                }

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    // Attempt rollback;
                    device.Rename(tempFilePath, mtpPath);
                }
                catch { }
                Console.WriteLine($"Upload error: {ex.Message}");

                return false;
            }
        }

        public List<string> List(string mtpPath)
        {
            mtpPath = RelativePath(mtpPath, out var deviceName);
            InitializeMtpDevice(deviceName);

            try
            {
                // Get the list of files/folders in the specified MTP directory
                return device.EnumerateFiles(mtpPath).Select(path => Path.GetFileName(path)).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"List error: {ex.Message}");
                return new List<string>();
            }
        }

        public bool IsConnected => initialized && device.IsConnected;

        public string RelativePath(string path)
        {
            return RelativePath(path, out var _);
        }

        public string RelativePath(string path, out string deviceName)
        {
            string mtpRelativePath;

            if (path.StartsWith(@"mtp://") )
            {
                var uri = new Uri(path);
                var segments = uri.Segments;

                deviceName = segments[1];


                mtpRelativePath = path.Substring( (@"mtp://" + segments[1]).Length);

            }
            else if (path.StartsWith("This PC" + Path.DirectorySeparatorChar))
            {

                var segments = path.Split(Path.DirectorySeparatorChar);

                deviceName = segments[1];

                mtpRelativePath = path.Substring(("This Pc" + Path.DirectorySeparatorChar + segments[1] + Path.DirectorySeparatorChar).Length);

            }
            else
            {
                deviceName = string.Empty;
                return path;
            }

            return mtpRelativePath;
        }
    }

}

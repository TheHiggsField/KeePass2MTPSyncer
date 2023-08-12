using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gio
{
    public class GioClient: MTPSync.MTPAccess
    {
        public List<string> Output {get; set;} = new List<string>();
        public List<string> Error {get; set;} = new List<string>();

        private string aRandomMtpPath = "SomethingWhichWilCauseAnError";

        public GioClient(string path)
        {
            aRandomMtpPath = path;
        }

        public bool Copy(string SourcePath, string destinationPath)
        {
            return Run($"copy \"{SourcePath}\" \"{destinationPath}\"");
        }

        public bool Download(string mtpPath, string destinationPath)
        {
            return Copy(mtpPath, destinationPath);
        }

        public bool Upload(string SourcePath, string mtpPath)
        {
            return Copy(SourcePath, mtpPath);
        }

        public List<string> List(string mtpPath)
        {
            return Run($"list \"{mtpPath}\"") ? Output : Error;

        }

        public bool Info(string mtpPath)
        {
            return Run($"info \"{mtpPath}\"");

        }

        public bool Run(string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "gio",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = new Process
                {
                    StartInfo = startInfo
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if(!string.IsNullOrEmpty(e.Data))
                    {
                        Output.Add(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if(!string.IsNullOrEmpty(e.Data))
                    {
                        Error.Add(e.Data);
                    }
                };

                Output.Clear();
                Error.Clear();

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }

        public bool IsConnected => Info(aRandomMtpPath); 
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTPSync
{
    public class GioClient: IMTPClient
    {
        public List<string> Output {get; set;} = new List<string>();
        public List<string> Error {get; set;} = new List<string>();

        private string mtpUriFromInitialize = string.Empty;

        public GioClient(string mtpUri)
        {
            Debug.Assert(Run("--version"), "The plugin only works for Linux systems, with the GIO CLI.");
            mtpUriFromInitialize += mtpUri;
        }


        public bool Copy(string mtpSourcePath, string destinationPath)
        {
            return Run($"copy \"{mtpSourcePath}\" \"{destinationPath}\"");
        }

        public bool Download(string mtpSourcePath, string destinationPath)
        {
            return Run($"copy \"{mtpSourcePath}\" \"{destinationPath}\"");
        }

        public bool Upload(string mtpSourcePath, string destinationPath)
        {
            return Run($"copy \"{mtpSourcePath}\" \"{destinationPath}\"");
        }

        public List<string> List(string mtpSourcePath)
        {
            return Run($"list \"{mtpSourcePath}\"") ? Output : Error;
        }

        public bool IsFolder(string mtpSourcePath)
        {
            List(mtpSourcePath);
            return Error.Count == 0;
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

        public bool IsConnected => Run(@"info " + mtpUriFromInitialize); 
    }
}
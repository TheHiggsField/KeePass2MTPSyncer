using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gio
{
    public class GioClient: MTPSync.MTPAccess
    {
        public List<string> Output {get; set;} = new List<string>();
        public List<string> Error {get; set;} = new List<string>();

        public bool Move(string mtpSourcePath, string destinationPath)
        {
            return Run($"move \"{mtpSourcePath}\" \"{destinationPath}\"");
        }

        public bool Copy(string mtpSourcePath, string destinationPath)
        {
            return Run($"copy \"{mtpSourcePath}\" \"{destinationPath}\"");
        }

        public List<string> List(string mtpSourcePath)
        {
            return Run($"list \"{mtpSourcePath}\"") ? Output : Error;

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
    }
}
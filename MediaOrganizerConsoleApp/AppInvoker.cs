using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MediaOrganizerConsoleApp
{
     public class AppInvoker
    {
        private static readonly char ArgumentEscapeChar = ' ';

        protected AppInvoker() { }

        protected static RunProcessResults RunProcess(string binaryName, IEnumerable<string> args, ILogWriter logger,
            string workingPath = null)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = binaryName,
                    Arguments = ConvertToArgs(args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingPath
                }
            };

            logger.WriteLog($"Executing {proc.StartInfo.FileName} {proc.StartInfo.Arguments}...", true);

            try
            {
                proc.Start();
                proc.WaitForExit();

                return new RunProcessResults(proc);
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Error executing process: {ex.Message}", true);
                throw;
            }
        }

        protected static string ConvertToArgs(IEnumerable<string> input)
        {
            StringBuilder output = new StringBuilder();
            foreach (string arg in input)
            {
                if (arg.Contains(' '))
                {
                    output.Append(ArgumentEscapeChar);
                    output.Append(arg);
                    output.Append(ArgumentEscapeChar);
                }
                else
                {
                    output.Append(arg);
                }
                output.Append(' ');
            }
            return output.ToString();
        }

        protected class RunProcessResults
        {
            public RunProcessResults() { }

            public RunProcessResults(Process proc)
            {
                if (!proc.HasExited)
                {
                    throw new ArgumentException("Cannot create a RunProcessResults instance on a process that is still running.");
                }

                ExitCode = proc.ExitCode;
                StandardOutput = proc.StandardOutput.ReadToEnd();
                StandardError = proc.StandardError.ReadToEnd();
                CommandLine = proc.StartInfo.FileName + " " + proc.StartInfo.Arguments;
            }

            public int ExitCode { get; private set; }
            public string StandardOutput { get; private set; }
            public string StandardError { get; private set; }
            public string CommandLine { get; private set; }
        }

    }

    public class AppInvokerException : Exception
    {
        public AppInvokerException(string message) : base(message)
        {
        }

        public AppInvokerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public string StandardOutput { get; internal set; }
        public string StandardError { get; internal set; }
        public int ExitCode { get; internal set; }
        public string CommandLine { get; internal set; }
    }


}

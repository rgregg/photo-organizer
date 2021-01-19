using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    /// <summary>
    /// Implements a basic file scan that enumerates all of the files in a 
    /// directory. Optionally supports recursive behavior.
    /// </summary>
    public abstract class RecursiveFileScanner
    {
        public DirectoryInfo OriginalSource { get; protected set; }
        public bool Recursive { get; protected set; }
        public bool Verbose { get; protected set; }

        public string LogFile { get; protected set; }

        private StreamWriter LogFileStream { get; set; }

        public RecursiveFileScanner(DirectoryInfo source, bool recursive, bool verbose, string logFile = null)
        {
            OriginalSource = source;
            Recursive = recursive;
            Verbose = verbose;
            LogFile = logFile;
        }

        public virtual void Scan()
        {
            if (!string.IsNullOrEmpty(LogFile))
            {
                StreamWriter logStream = new StreamWriter(LogFile, true);
                LogFileStream = logStream;
                logStream.WriteLine($"{DateTime.Now} - Starting scan: ${OriginalSource.FullName}.");
            }

            if (OriginalSource.Exists)
            {
                ScanDirectory(OriginalSource);
            }
            else
            {
                WriteLog($"Unable to locate directory {OriginalSource.FullName}");
            }

            if (null != LogFileStream)
            {
                LogFileStream.Flush();
                LogFileStream.Dispose();
                LogFileStream = null;
            }
        }

        protected virtual void ScanDirectory(DirectoryInfo source)
        {
            WriteLog($"Scanning directory: {source.FullName}", true);

            var files = source.EnumerateFiles();
            foreach (var file in files)
            {
                ScanFile(file);
            }

            if (Recursive)
            {
                var folders = source.EnumerateDirectories();
                foreach (var folder in folders)
                {
                    ScanDirectory(folder);
                }
            }
        }

        protected virtual void ScanFile(FileInfo file)
        {
            WriteLog($"Scanning file: {file.Name}", true);
        }

        protected virtual void WriteLog(string message, bool verbose = false)
        {
            if (!verbose || Verbose)
            {
                Console.WriteLine(message);
                if (null != LogFileStream)
                {
                    LogFileStream.WriteLine($"{DateTime.Now} - {message}");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizerConsoleApp
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
        protected ILogWriter LogWriter { get; set; }
        private FileTypeRecognizer Recognizer { get; set; }

        public RecursiveFileScanner(DirectoryInfo source, bool recursive, bool verbose, bool useDeepInspection, ILogWriter logWriter)
        {
            OriginalSource = source;
            Recursive = recursive;
            Verbose = verbose;
            LogWriter = logWriter;
            Recognizer = new FileTypeRecognizer(useDeepInspection, logWriter);
        }

        public virtual void Scan()
        {
            if (OriginalSource.Exists)
            {
                ScanDirectory(OriginalSource);
            }
            else
            {
                WriteLog($"Unable to locate directory {OriginalSource.FullName}");
            }
        }

        protected virtual void ScanDirectory(DirectoryInfo source)
        {
            WriteLog($"Scanning directory: {source.FullName}", false);

            var files = source.EnumerateFiles();
            foreach (var file in files)
            {
                var fileSignature = Recognizer.DetectSignature(file);

                if (!IsExcludedFile(file, fileSignature))
                {
                    ScanFile(file, fileSignature);
                }
                else
                {
                    WriteLog($"Skipping {file.Name} -- on excluded extension list.", true);
                }
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

        protected virtual void ScanFile(FileInfo file, FormatSignature signature)
        {
            WriteLog($"Scanning file: {file.Name} of type {signature.Type}", true);
        }

        protected virtual bool IsExcludedFile(FileInfo file, FormatSignature signature)
        {
            return signature.Excluded;
        }

        protected virtual void WriteLog(string message, bool verbose = false)
        {
            var writer = LogWriter;
            if (null != writer)
            {
                writer.WriteLog(message, verbose);
            }
        }
    }
}

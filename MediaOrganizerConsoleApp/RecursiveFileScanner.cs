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
        protected ILogWriter LogWriter { get; private set; }
        protected FileTypeRecognizer Recognizer { get; private set; }

        /// <summary>
        /// Creates a new instance of a recursive file scanner.
        /// </summary>
        /// <param name="source">Directory where the files to be scanned reside.</param>
        /// <param name="recursive">Iterate through the subdirectories of the source directory.</param>
        /// <param name="verbose">Output verbose status messages</param>
        /// <param name="useDeepInspection">Look at the binary contents of the file to determine the file type, beyond just the extension.</param>
        /// <param name="logWriter">Output logger for stauts messages and errors.</param>
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
            var files = source.EnumerateFiles();

            if (StartProcessingDirectory(source, files))
            {
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
            }

            FinishedScanningFilesInDirectory(source, files);

            if (Recursive)
            {
                var folders = source.EnumerateDirectories();
                foreach (var folder in folders)
                {
                    ScanDirectory(folder);
                }
            }
        }

        protected virtual bool StartProcessingDirectory(DirectoryInfo source, IEnumerable<FileInfo> files)
        {
            WriteLog($"Scanning directory: {source.FullName} containing {files.Count()} file(s).", false);
            return true;
        }

        protected virtual void FinishedScanningFilesInDirectory(DirectoryInfo source, IEnumerable<FileInfo> files)
        {

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

using System;
using System.IO;
using System.Linq;

namespace MediaOrganizerConsoleApp.Commands
{
    /// <summary>
    /// Verifies that the binary format of files matches the file extension for the file.

    /// </summary>
    public class ScanMedia : RecursiveFileScanner
    {
        public bool InteractiveMode { get; set; }

        public Program.AnswerMode AutoAnswer { get; set; }

        public ScanMedia(DirectoryInfo source, ScanCommandOptions opts, ILogWriter logWriter)
            : base(source, opts.Recursive, opts.VerboseOutput, true, logWriter)
        {
            InteractiveMode = true;
            if (opts.DefaultToNo)
            {
                AutoAnswer = Program.AnswerMode.AutoNo;
            }
            else if (opts.DefaultToYes)
            {
                AutoAnswer = Program.AnswerMode.AutoYes;
            }
        }

        protected override void ScanFile(FileInfo file, FormatSignature signature)
        {
            base.ScanFile(file, signature);

            // Check to see if file extension matches file type
            if (signature.Excluded)
            {
                WriteLog($"Skipping {file.Name} -- on excluded list", true);
                return;
            }

            string expectedFileExtension = signature.Extensions.First();
            if (!string.IsNullOrEmpty(expectedFileExtension))
            {
                if (!file.Extension.Equals(expectedFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    if (InteractiveMode)
                    {
                        // Extension doesn't match expected value
                        if (Program.AskYesOrNoQuestion($"File {file.Name} is {signature.Format} -- rename to correct extension ({expectedFileExtension})?", true, AutoAnswer))
                        {
                            RenameFileExtension(file, expectedFileExtension);
                        }
                    }
                    else
                    {
                        WriteLog($"File {file.Name} is {signature.Format} -- file extension should be {expectedFileExtension} instead.");
                    }
                }
                else
                {
                    WriteLog($"File {file.Name} is {signature.Format} -- file extension matches.", true);
                }
            }
        }

        /// <summary>
        /// Renames a file to use the new file extension.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newExtension">The next extension to append. Must start with a period.</param>
        private void RenameFileExtension(FileInfo file, string newExtension)
        {
            if (!newExtension.StartsWith("."))
            {
                throw new ArgumentException("newExtension must start with a period (.).");
            }
            if (null == file)
            {
                throw new ArgumentNullException("file");
            }

            // TODO: Implement renaming a file
            var newFileName = Path.ChangeExtension(file.FullName, newExtension);
            if (!File.Exists(newFileName))
            {
                file.MoveTo(newFileName);
            }
            else
            {
                // Destination exists -- what should we do?
                Console.WriteLine($"Tried to rename file to {newFileName}, but it already exists.");
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    /// <summary>
    /// Verifies that the binary format of files matches the file extension for the file.

    /// </summary>
    public class BinaryFormatScanner : RecursiveFileScanner
    {
        public bool InteractiveMode { get; set; }
        public bool AutoAnswerYes { get; set; }
        public bool AutoAnswerNo { get; set; }

        public BinaryFormatScanner(ScanCommandOptions opts, ILogWriter logWriter)
            : base(new DirectoryInfo(opts.SourceFolder), opts.Recursive, opts.VerboseOutput, true, logWriter)
        {
            InteractiveMode = true;
            AutoAnswerYes = opts.DefaultToYes;
            AutoAnswerNo = opts.DefaultToNo;
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
                        if (AskYesOrNoQuestion($"File {file.Name} is {signature.Format} -- rename to correct extension ({expectedFileExtension})?", true))
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

        private bool AskYesOrNoQuestion(string prompt, bool defaultIsYes)
        {
            Console.Write(prompt);
            if(defaultIsYes)
            {
                Console.Write(" [Y/n]: ");
            }
            else
            {
                Console.Write(" [y/N]: ");
            }

            if (AutoAnswerNo)
            {
                Console.WriteLine("N");
                return false;
            }
            else if (AutoAnswerYes)
            {
                Console.WriteLine("Y");
                return true;
            }
            else
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    return defaultIsYes;
                }

                if (line.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (line.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Console.WriteLine("Invalid input. Try again.");
                return AskYesOrNoQuestion(prompt, defaultIsYes);
            }
        }
    }
}

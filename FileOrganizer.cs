using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    class FileOrganizer : DirectoryAndFileEnumerator<DateTimeOffset?>
    {
        public DirectoryInfo Destination { get; private set; }

        public OrganizedFilesAction Options { get; set; }

        public FileOrganizer(DirectoryInfo destination, OrganizedFilesAction opts, ParsedFileCache cache) : base(opts, cache)
        {
            this.Destination = destination;
            this.Options = opts;
        }

        protected override void ProcessFile(FileInfo file, ref DateTimeOffset? suggestion)
        {
            var info = ParseFile(file);

            bool moveThisFile = info.Type == MediaType.Image || info.Type == MediaType.Video;

            string action = moveThisFile ? Options.CopyInsteadOfMove ? "Copy" : "Move" : "Skipped";

            DateTimeOffset? dateTaken = info.Taken;
            if (!dateTaken.HasValue && info.Type == MediaType.Video && suggestion.HasValue)
            {
                if (Opts.VerboseOutput) Console.WriteLine("Infering date for {0} as {1}", file.Name, suggestion);
                dateTaken = suggestion;
            }

            if (moveThisFile && dateTaken.HasValue)
            {
                string targetFullName = dateTaken.Value.ToString(Options.DirectoryFormat);
                DirectoryInfo targetDirectory = new DirectoryInfo(Path.Combine(this.Destination.FullName, targetFullName));
                string targetPath = Path.Combine(targetDirectory.FullName, file.Name);

                // Write verbose output about what we're doing
                Console.WriteLine("{0}: {1}\t{2}\t{3}\t{4}\t{5}", action, file.Name, info.Type, info.Taken, info.CameraMake, info.CameraModel);

                if (this.Options.VerboseOutput)
                {
                    if (this.Options.CopyInsteadOfMove)
                    {
                        Console.WriteLine("Copying {0} to {1}", file.Name, targetPath);
                    }
                    else
                    {
                        Console.WriteLine("Moving {0} to {1}", file.Name, targetPath);
                    }
                }

                // Check to see if the destination already exists
                FileInfo destination = new FileInfo(targetPath);
                if (destination.Exists)
                {
                    FileAlreadyExists(file, targetPath);
                }
                else
                {
                    if (!Options.Simulate)
                    {
                        targetDirectory.Create();
                        DoFileAction(file, targetPath);
                    }
                }
            }
            else if (Options.VerboseOutput && moveThisFile)
            {
                Console.WriteLine("Skipping file (no date taken): " + info.Filename);
            }
            else if (Options.VerboseOutput)
            {
                Console.WriteLine("Skipping file (not included type): " + info.Filename);
            }

            // Pass our current value on to be used later if required
            suggestion = dateTaken;
        }


        private void FileAlreadyExists(FileInfo sourceFile, string targetPath)
        {
            switch (Options.ConflictBehavior)
            {
                case ExistingFileMode.Skip:
                    {
                        Console.WriteLine("Skipping file (already exists): " + sourceFile.Name);
                        break;
                    }
                case ExistingFileMode.Overwrite:
                    {
                        FileInfo targetFile = new FileInfo(targetPath);
                        try
                        {
                            VerboseLog("File already exists: Overwriting destination file");

                            if (!Options.Simulate)
                            {
                                targetFile.Delete();
                                DoFileAction(sourceFile, targetPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Skipping file (error after already exists): " + ex.Message);
                        }
                        break;
                    }
                case ExistingFileMode.Rename:
                    {
                        VerboseLog("File already exists: Renaming on copy to destination");

                        if (!Options.Simulate)
                        {
                            FileInfo targetFile = new FileInfo(targetPath);
                            DirectoryInfo targetDirectory = targetFile.Directory;
                            string renamedTargetPath = GenerateNewTargetFileName(targetPath, targetDirectory);
                            DoFileAction(sourceFile, renamedTargetPath);
                        }
                        break;
                    }
                case ExistingFileMode.Delete:
                    {
                        if (FilesAreIdentifical(sourceFile, new FileInfo(targetPath)))
                        {
                            VerboseLog("File already exists: deleting source file.");
                            if (!Options.Simulate)
                            {
                                sourceFile.Delete();
                            }
                        }
                        else
                        {
                            VerboseLog("Files were not identical, so the original was not deleted.");
                        }
                        break;
                    }
                default:
                    throw new NotSupportedException("Unsupported file behavior setting");
            }
        }

        private static string GenerateNewTargetFileName(string targetPath, DirectoryInfo targetDirectory)
        {
            int incrementValue = 2;
            string targetFileName = Path.GetFileNameWithoutExtension(targetPath);
            
            string renamedTargetPath = null;
            if (targetFileName.Length >= 2)
            {
                string endOfFileName = targetFileName.Substring(targetFileName.Length - 2);
                if (endOfFileName[0] == '-' && char.IsNumber(endOfFileName[1]))
                {
                    incrementValue = int.Parse(endOfFileName[1].ToString()) + 1;
                    string newFileName = targetFileName.Substring(0, targetFileName.Length - 2) + string.Format("-{0}", incrementValue);
                    renamedTargetPath = Path.Combine(targetDirectory.FullName, newFileName + Path.GetExtension(targetPath));
                }
            }
            
            if (string.IsNullOrEmpty(renamedTargetPath))
            {
                renamedTargetPath = Path.Combine(targetDirectory.FullName, Path.GetFileNameWithoutExtension(targetPath) + string.Format("-{0}", incrementValue) + Path.GetExtension(targetPath));
            }
             
            return renamedTargetPath;
        }

        private void DoFileAction(FileInfo file, string targetPath)
        {
            try
            {
                if (this.Options.CopyInsteadOfMove)
                {
                    file.CopyTo(targetPath);
                }
                else
                {
                    file.MoveTo(targetPath);
                }
            }
            catch (IOException ioex)
            {
                switch (ioex.HResult)
                {
                    case -2147024816:   // File already exists
                    case -2147024713:
                        FileAlreadyExists(file, targetPath);
                        break;
                    default:
                        Console.WriteLine("File skipped (IOException {0}): {1}\n{2}", ioex.HResult, file.Name, ioex.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }

    }
}

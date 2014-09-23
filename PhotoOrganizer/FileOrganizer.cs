using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class FileOrganizer
    {
        public DirectoryInfo Destination { get; private set; }

        CommandLineOptions Options { get; set; }

        public FileOrganizer(DirectoryInfo destination)
        {
            this.Destination = destination;
        }

        public FileOrganizer(DirectoryInfo destination, CommandLineOptions opts) : this(destination)
        {
            this.Options = opts;
        }


        public void ProcessSourceFolder(DirectoryInfo source)
        {
            if (Options.VerboseOutput)
                Console.WriteLine("Process source folder {0}.", source.FullName);

            var files = source.EnumerateFiles();
            if (this.Options.RunInParallel)
            {
                Parallel.ForEach(files, file => ProcessFile(file));
            }
            else
            {
                DateTime? previousDateTime = null;
                foreach (var file in files)
                {
                    previousDateTime = ProcessFile(file, previousDateTime);
                }
            }

            if (this.Options.Recursive)
            {
                var folders = source.EnumerateDirectories();
                if (Options.RunInParallel)
                {
                    Parallel.ForEach(folders, folder => ProcessSourceFolder(folder));
                }
                else
                {
                    foreach (var folder in folders) 
                    { 
                        ProcessSourceFolder(folder); 
                    }
                }
            }
        }

        private DateTime? ProcessFile(FileInfo file, DateTime? suggestion = null)
        {
            var attributes = new DetailFileInfo.FileAttributes[] { DetailFileInfo.FileAttributes.PerceivedType, DetailFileInfo.FileAttributes.DateTaken, 
                    DetailFileInfo.FileAttributes.CameraMaker, DetailFileInfo.FileAttributes.CameraModel };

            DetailFileInfo.CFileInfo info = new DetailFileInfo.CFileInfo(file.FullName, attributes);
            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", file.Name, info.PerceivedType, info.DateTaken, info.CameraMake, info.CameraModel);

            bool moveThisFile = (this.Options.ActOnImages && info.PerceivedType == DetailFileInfo.PerceivedFileType.Image) ||
                                (this.Options.ActOnVideos && info.PerceivedType == DetailFileInfo.PerceivedFileType.Video);

            DateTime? dateTaken = info.DateTaken;
            if (!dateTaken.HasValue && info.PerceivedType == DetailFileInfo.PerceivedFileType.Video && this.Options.InferVideoDate && suggestion.HasValue)
            {
                if (this.Options.VerboseOutput) Console.WriteLine("Infering date for {0} as {1}", file.Name, suggestion);
                dateTaken = suggestion;
            }

            if (moveThisFile && dateTaken.HasValue)
            {
                string targetFullName = dateTaken.Value.ToString(Options.DirectoryFormat);
                DirectoryInfo targetDirectory = new DirectoryInfo(Path.Combine(this.Destination.FullName, targetFullName));
                string targetPath = Path.Combine(targetDirectory.FullName, file.Name);

                if (this.Options.VerboseOutput) Console.WriteLine("Moving {0} to {1}", file.Name, targetPath);
                if (!this.Options.Simulate)
                {
                    try
                    {
                        targetDirectory.Create();
                        DoFileAction(file, targetPath);
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
            else if (this.Options.VerboseOutput && moveThisFile)
            {
                Console.WriteLine("Skipping file (no date taken): " + info.FileName);
            }
            else if (this.Options.VerboseOutput)
            {
                Console.WriteLine("Skipping file (not included type): " + info.FileName);
            }

            return dateTaken;
        }

        private void VerboseLog(string message)
        {
            if (Options.VerboseOutput)
                Console.WriteLine(message);
        }

        private bool FilesAreIdentifical(FileInfo source, FileInfo destination)
        {
            if (source.Name != destination.Name)
            {
                VerboseLog("Match failed: different filenames");
                return false;
            }

            if (source.Length != destination.Length)
            {
                VerboseLog("Match failed: different sizes");
                return false;
            }

            var source_crc = GetChecksum(source);
            var dest_crc = GetChecksum(destination);
            if (source_crc != dest_crc)
            {
                VerboseLog("Match failed: different hashes");
                return false;
            }

            VerboseLog("Match succeeded. Files are the same.");
            return true;
        }

        private string GetChecksum(FileInfo file)
        {
            int buffer_size = 1024 * 1024;
            using (BufferedStream stream = new BufferedStream(file.OpenRead(), buffer_size))
            {
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] checksum = md5.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }

        private void FileAlreadyExists(FileInfo sourceFile, string targetPath)
        {
            switch (Options.ExistingFileBehavior)
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

                            targetFile.Delete();
                            DoFileAction(sourceFile, targetPath);
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

                        FileInfo targetFile = new FileInfo(targetPath);
                        DirectoryInfo targetDirectory = targetFile.Directory;
                        string renamedTargetPath = GenerateNewTargetFileName(targetPath, targetDirectory);
                        
                        DoFileAction(sourceFile, renamedTargetPath);
                        break;
                    }
                case ExistingFileMode.DeleteSourceFile:
                    {
                        if (!Options.VerifyFilesAreIdentical || FilesAreIdentifical(sourceFile, new FileInfo(targetPath)))
                        {
                            VerboseLog("File already exists: deleting source file.");
                            sourceFile.Delete();
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
            if (this.Options.CopyInsteadOfMove)
                file.CopyTo(targetPath);
            else
                file.MoveTo(targetPath);
        }

    }
}

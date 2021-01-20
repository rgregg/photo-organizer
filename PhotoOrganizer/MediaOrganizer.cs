using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class MediaOrganizer : RecursiveFileScanner
    {
        public DirectoryInfo Destination { get; private set; }
        public bool UseCache { get; set; }
        public ParserCache Cache { get; private set; }
        public OrganizationMode Action { get; set; }
        public string DestinationDirectoryFormat { get; private set; }
        public List<MediaType> MediaTypesToOrganizer { get; private set; }
        public ExistingFileMode ActionOnExistingFile { get; private set; }

        public enum OrganizationMode
        {
            CopyToDestination,
            MoveToDestination,
            Simulation
        }

        public MediaOrganizer(DirectoryInfo source, DirectoryInfo destination, OrganizeCommandLineOptions opts, ParserCache cache, ILogWriter logWriter) 
            : base(source, opts.Recursive, opts.VerboseOutput, false, logWriter)
        {
            Cache = cache;
            Destination = destination;

            if (opts.Simulate)
            {
                Action = OrganizationMode.Simulation;
            }
            else if (opts.CopyInsteadOfMove)
            {
                Action = OrganizationMode.CopyToDestination;
            }
            else
            {
                Action = OrganizationMode.MoveToDestination;
            }
            
            UseCache = opts.CacheFileInfo;
            DestinationDirectoryFormat = opts.DirectoryFormat;
            MediaTypesToOrganizer = new List<MediaType> { MediaType.Image, MediaType.Video };
            ActionOnExistingFile = opts.ConflictBehavior;
        }

        protected DateTimeOffset? LastKnownFileDate { get; set; }

        protected override void ScanFile(FileInfo file, FormatSignature signature)
        {
            base.ScanFile(file, signature);

            MediaMetadata metadata = GetMediaInfo(file, signature);
            bool performAction = MediaTypesToOrganizer.Contains(signature.Type);
            if (!performAction)
            {
                WriteLog($"Skipping {file.Name} -- not a supported media type: {signature.Type}.");
                return;
            }

            // Attempt to infer date for files that don't have one
            DateTimeOffset? mediaDate = metadata.DateCaptured;
            if (!mediaDate.HasValue && LastKnownFileDate.HasValue && signature.Type == MediaType.Video)
            {
                WriteLog($"Infering date for {file.Name} as {LastKnownFileDate.Value}");
                mediaDate = LastKnownFileDate.Value;
            }

            if (!mediaDate.HasValue)
            {
                WriteLog($"Skipping {file.Name} -- no media date was available.");
                return;
            }

            string targetFullName = mediaDate.Value.ToString(DestinationDirectoryFormat);
            DirectoryInfo targetDirectory = new DirectoryInfo(Path.Combine(Destination.FullName, targetFullName));
            string targetPath = Path.Combine(targetDirectory.FullName, file.Name);

            WriteActivity(file, metadata, targetPath, signature);

            // Check to see if the destination already exists
            FileInfo destination = new FileInfo(targetPath);
            if (destination.Exists)
            {
                HandleExistingFile(file, targetPath);
            }
            else
            {
                switch (Action)
                {
                    case OrganizationMode.MoveToDestination:
                    case OrganizationMode.CopyToDestination:
                        targetDirectory.Create();
                        PerformItemAction(file, targetPath);
                        break;

                    case OrganizationMode.Simulation:
                        // Nothing to do
                        break;
                }
            }
            LastKnownFileDate = mediaDate;
        }

        private void WriteActivity(FileInfo file, MediaMetadata info, string targetPath, FormatSignature signature)
        {
            WriteLog($"{Action}: {file.Name}\t{signature.Type}\t{info.DateCaptured}\t{info.CameraMake}\t{info.CameraModel}");

            if (Action == OrganizationMode.CopyToDestination)
            {
                WriteLog($"Copying {file.Name} to {targetPath}", true);
            }
            else if (Action == OrganizationMode.MoveToDestination)
            {
                WriteLog($"Moving {file.Name} to {targetPath}", true);
            }
        }

        private MediaMetadata GetMediaInfo(FileInfo file, FormatSignature signature)
        {
            MediaMetadata metadata = null;
            MediaType type = MediaType.Unknown;
            BinaryFormat format = BinaryFormat.Unknown;

            // Load cached data for the file, if available
            if (UseCache && Cache.TryCacheLookup(file, out CacheEntry entry))
            {
                WriteLog($"Loaded cached data for {file.Name}.", true);
                metadata = entry.Metadata;
                type = entry.Type;
                format = entry.Format;
            }

            // Calculate data if necessary
            LogWriter.WriteLog($"Metadata: Found a record for {file.Name}: {metadata}, {type}, {format}", true);
            if (null == metadata || type == MediaType.Unknown || format == BinaryFormat.Unknown)
            {
                metadata = MediaMetadataReader.ParseFile(file, LogWriter);
                if (UseCache) 
                {
                    Cache.Add(file, metadata, signature); 
                }
            }
            return metadata;
        }

        /// <summary>
        /// Check to see if the source and destination files are identical.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        private bool CheckFilesAreIdentical(FileInfo source, FileInfo destination)
        {
            // Check filenames
            if (!source.Name.Equals(destination.Name, StringComparison.OrdinalIgnoreCase))
            {
                WriteLog($"Match failed: filenames are different.", true);
                return false;
            }

            // Check file lengths
            if (source.Length != destination.Length)
            {
                WriteLog($"Match failed: files have different sizes.", true);
                return false;
            }

            // Compute hashes for each file and compare
            WriteLog($"Calculating hashes for files...", true);
            var source_crc = CalculateFileHash(source);
            var dest_crc = CalculateFileHash(destination);
            if (source_crc != dest_crc)
            {
                WriteLog($"Match failed: hashes do not match.", true);
                return false;
            }

            WriteLog($"Match successful: files are identical.", true);
            return true;
        }

        /// <summary>
        /// Computes an MD5 hash for the contents of a file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string CalculateFileHash(FileInfo file)
        {
            int buffer_size = 1024 * 1024;
            using (BufferedStream stream = new BufferedStream(file.OpenRead(), buffer_size))
            {
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] checksum = md5.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }

        private void HandleExistingFile(FileInfo sourceFile, string targetPath)
        {
            switch (ActionOnExistingFile)
            {
                case ExistingFileMode.Skip:
                    {
                        WriteLog($"Skipping {sourceFile.Name} -- destination file exists.");
                        return;
                    }
                case ExistingFileMode.Overwrite:
                    {
                        FileInfo targetFile = new FileInfo(targetPath);
                        WriteLog($"Overwriting existing destination file {sourceFile.Name}.");
                        try
                        {
                            PerformItemAction(sourceFile, targetPath, true);
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"Skipping {sourceFile.Name} -- error with existing file: {ex.Message}");
                        }
                        break;
                    }
                case ExistingFileMode.Rename:
                    {
                        WriteLog($"File {sourceFile.Name} already exists: renaming in destination.", true);
                        FileInfo targetFile = new FileInfo(targetPath);
                        DirectoryInfo targetDirectory = targetFile.Directory;
                        string renamedTargetPath = GenerateNewTargetFileName(targetPath, targetDirectory);
                        PerformItemAction(sourceFile, renamedTargetPath);
                        break;
                    }
                case ExistingFileMode.Delete:
                    {
                        bool filesAreIdentical = CheckFilesAreIdentical(sourceFile, new FileInfo(targetPath));
                        if (filesAreIdentical)
                        {
                            WriteLog($"Deleting {sourceFile.Name} -- identical file exists in destination.");
                            if (Action != OrganizationMode.Simulation)
                            {
                                sourceFile.Delete();
                            }
                        }
                        else
                        {
                            WriteLog($"Skipping {sourceFile.Name} -- file exists but was not identical.");
                        }
                        break;
                    }
                default:
                    throw new NotSupportedException($"Unsupported existing file behavior: {ActionOnExistingFile}");
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

        private void PerformItemAction(FileInfo file, string targetPath, bool overwrite = false)
        {
            try
            {
                switch (Action)
                {
                    case OrganizationMode.CopyToDestination:
                        file.CopyTo(targetPath, overwrite);
                        break;
                    case OrganizationMode.MoveToDestination:
                        if (overwrite)
                        {
                            FileInfo destInfo = new FileInfo(targetPath);
                            if (destInfo.Exists)
                            {
                                destInfo.Delete();
                            }
                        }
                        file.MoveTo(targetPath);
                        break;
                }
            }
            catch (IOException ioex)
            {
                switch (ioex.HResult)
                {
                    case -2147024816:   // File already exists
                    case -2147024713:
                        if (!overwrite)
                        {
                            HandleExistingFile(file, targetPath);
                        }
                        else
                        {
                            WriteLog($"Skipping {file.Name} -- destination exists and could not be overwritten.");
                        }
                        break;
                    default:
                        WriteLog($"Skipping {file.Name} -- error [{ioex.HResult}]: {ioex.Message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Skipping {file.Name} -- error {ex.Message}");
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableParser;

namespace MediaOrganizerConsoleApp.Commands
{
    internal class DedupeChecker : RecursiveFileScanner
    {
        private DupeCheckerCommandOptions Options { get; set; }
        private ParserCache Cache { get; set; }

        public DedupeChecker(DirectoryInfo source, DupeCheckerCommandOptions opts, ParserCache cache, ILogWriter logWriter)
            : base(source, opts.Recursive, opts.VerboseOutput, true, logWriter)
        {
            this.Cache = cache;
            this.Options = opts;
        }

        protected override bool StartProcessingDirectory(DirectoryInfo directory, IEnumerable<FileInfo> files)
        {
            base.StartProcessingDirectory(directory, files);

            string[] skipList = { ".xmp" };

            // Assign files to groups based on their filename key
            Dictionary<string, List<FileInfo>> matches = new Dictionary<string, List<FileInfo>>();
            foreach (var file in files)
            {
                if (skipList.Contains(file.Extension.ToLower())) 
                {
                    continue;
                }

                var key = GenerateKey(file.Name);
                List<FileInfo> otherFiles;
                if (!matches.TryGetValue(key, out otherFiles))
                {
                    otherFiles = new List<FileInfo>();
                    matches[key] = otherFiles;
                }
                otherFiles.Add(file);
            }

            // For any key that matched multiple files, figure out what to do.
            foreach (var pair in matches)
            {
                if (pair.Value.Count > 1)
                {
                    CompareMatchingFiles(pair.Key, pair.Value);
                }
            }

            // Don't iterate through individual files
            return false;
        }

        private void CompareMatchingFiles(string key, IEnumerable<FileInfo> inputFiles)
        {
            var files = new List<DuplicateScore>(
                from f in inputFiles
                select new DuplicateScore(f).Parse(Recognizer, Options.CacheFileInfo ? Cache : null, LogWriter));

            if (Options.IgnoreLivePhotos && IsLivePhotoPair(files))
            {
                // Skip this pair as a duplicate
                LogWriter.WriteLine($"Skipping Live Photo: {key}");
                return;
            }
            
            DateTimeOffset? captured = null;
            foreach (var file in files)
            {
                ComputeExtensionScore(file);
                if (!captured.HasValue)
                {
                    captured = file.Metadata.DateCaptured;
                    file.IsDuplicate = true;
                }
                else if (captured.Value.Equals(file.Metadata.DateCaptured))
                {
                    file.IsDuplicate = true;
                }
                else if (!file.Metadata.DateCaptured.HasValue || string.IsNullOrEmpty(file.Metadata.CameraModel))
                {
                    file.IsDuplicate = false;
                    file.Score *= 0.1;
                }
            }
            ComputeFileSizeScore(files);

            var winner = files.FirstOrDefault();
            foreach (var file in files)
            {
                if (file.Score > winner.Score)
                {
                    winner = file;
                }
            }

            ChooseDuplicateWinner(key, files, winner);

        }

        private static bool IsLivePhotoPair(List<DuplicateScore> files)
        {
            var images = (from f in files
                          where f.Signature.Type == MediaType.Picture
                          select f);
            var movies = (from f in files
                          where f.Signature.Type == MediaType.Video && f.Signature.Format == BinaryFormat.QuickTime
                          select f);
            if (files.Count == 2 && images.Count() == 1 && movies.Count() == 1 &&
                string.Equals(Path.GetFileNameWithoutExtension(images.First().FileInfo.Name),
                              Path.GetFileNameWithoutExtension(movies.Single().FileInfo.Name)))
            {
                return true;
            }
            return false;
        }

        private void ChooseDuplicateWinner(string key, List<DuplicateScore> files, DuplicateScore winner)
        {
            Console.WriteLine();
            LogWriter.WriteLog($"Duplicate evaluation for {key}", false);
            LogWriter.WriteLog($" -- Winner: {winner.FileInfo.Name}", false);

            var table = files.ToStringTable(new string[] { "ID", "Name", "Format", "Size", "Score", "Dupe", "Date Taken", "Camera Model" },
                f => $"{files.IndexOf(f)}" + (winner == f ? "*" : ""),
                f => f.FileInfo.Name,
                f => f.Signature.Format,
                f => f.FileInfo.Length,
                f => (int)(f.Score * 100),
                f => f.IsDuplicate ? "Y" : "N",
                f => f.Metadata.DateCaptured,
                f => f.Metadata.CameraModel
                );
            LogWriter.WriteLog(table, false);
            Console.Write($"[S]kip; or keep file with index [{files.IndexOf(winner)}]: ");
            var input = Console.ReadLine();

            int index;
            if (input.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                LogWriter.WriteLog("Skipping. No files deleted.", false);
                return;
            }
            else if (int.TryParse(input, out index) && index >= 0 && index < files.Count())
            {
                winner = files[index];
            }
            else if (string.IsNullOrEmpty(input))
            {
                // Keep existing winner
            }
            else
            {
                LogWriter.WriteLog($"Unable to recognize input: \"{input}\". No action performed.", false);
            }

            LogWriter.WriteLog($"Keeping {winner.FileInfo.Name}, removing other files.", false);
            DeleteNonWinnerFiles(winner, files);
        }

        private void DeleteNonWinnerFiles(DuplicateScore winner, IEnumerable<DuplicateScore> files)
        {
            var filesToRemove = new List<DuplicateScore>(files);
            if (!filesToRemove.Remove(winner))
            {
                // Unable to find the winner, so we skip
                LogWriter.WriteLog("Couldn't locate the winner, skipping delete operation. Try again later.", false);
                return;
            }

            foreach (var file in filesToRemove)
            {
                if (!Options.Simulate)
                {
                    LogWriter.WriteVerboseLine($"Deleting file {file.FileInfo.FullName}");
                    try
                    {
                        file.FileInfo.Delete();
                        LogWriter.WriteLine($"Deleted {file.FileInfo}.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.WriteLine($"Unable to delete file {file.FileInfo.FullName}: {ex.Message}");
                        LogWriter.WriteVerboseLine(ex.StackTrace);
                    }
                }
            }
        }

        private void ComputeFileSizeScore(List<DuplicateScore> files)
        {
            double meanFileSize = (from f in files select f.FileInfo.Length).Average();
            double meanFileNameLength = (from f in files select f.FileInfo.Name.Length).Average();
            foreach (var file in files)
            {
                // Prefer files that are larger than average size
                file.Score *= (file.FileInfo.Length / meanFileSize);
                file.Score *= (meanFileNameLength / file.FileInfo.Name.Length);
            }
        }

        private void ComputeExtensionScore(DuplicateScore file)
        {
            double score = file.Score;

            // Check to see if the extension matches the file format
            if (!FileExtensionMatchFormat(file))
            {
                score = 0;
            }

            switch (file.FileInfo.Extension.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    score *= 0.1;
                    break;
                case ".png":
                    score *= 0.2;
                    break;
                case ".heic":
                    score *= 0.8;
                    break;
                case ".cr2":
                case ".dng":
                    score *= 1.1;
                    break;
                default:
                    Console.WriteLine($"no score for {file.FileInfo.Extension}");
                    break;
            }
            file.Score = score;

        }

        

        private bool FileExtensionMatchFormat(DuplicateScore file)
        {
            // check to see if the file extension is correctly matching the format of the file.
            // sometimes we see CR2 files which are tagged as JPG, or JPG files tagged as CR2, or HEIC files tagged as JPG.
            return file.Signature.ExtensionMatches(file.FileInfo.Extension);
        }

        private class DuplicateScore
        {
            public FileInfo FileInfo { get; private set; }
            public MediaMetadata Metadata { get; private set; }
            public FormatSignature Signature { get; private set; }
            public double Score { get; set; }
            public bool IsDuplicate { get; set; }

            public DuplicateScore(FileInfo file)
            {
                FileInfo = file;
                Score = 1;
                IsDuplicate = false;
            }
            internal DuplicateScore Parse(FileTypeRecognizer recognizer, ParserCache cache, ILogWriter logWriter)
            {
                var signature = recognizer.DetectSignature(this.FileInfo);
                MediaMetadata metadata = CopyMedia.GetMediaInfo(this.FileInfo, signature, cache, logWriter);

                this.Metadata = metadata;
                this.Signature = signature;

                return this;
            }
        }

        private string GenerateKey(string filename)
        {
            // Reduce the filename to a consistent key value for possible iterations of a filename
            // e.g. IMG_1234 -> IMG_1234.jpg IMG_1234.CR2, IMG_1234-2.jpg, IMG_1234 (1).jpg

            // remove the extension
            var key = Path.GetFileNameWithoutExtension(filename).ToUpperInvariant();

            // look for trailing patterns
            var text = EndsWith(key, "-1", "-2", "-3", "-4", "-5", "-6", "-7", "-8", "-9",
                                    " (1)", " (2)", " (3)", " (4)", " (5)", " (6)", " (7)", " (8)", " (9)");
            if (text != null)
            {
                key = key.Substring(0, key.Length - text.Length);
            }
            return key;
        }

        private string EndsWith(string input, params string[] matches)
        {
            foreach (var match in matches)
            {
                if (input.EndsWith(match))
                {
                    return match;
                }
            }
            return null;
        }


    }
}

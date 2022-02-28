using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    internal class DedupeChecker : DirectoryAndFileEnumerator<string>
    {
        private DeduplicateFilesAction Options { get; set; }

        public DedupeChecker(DeduplicateFilesAction opts, ParsedFileCache cache) : base(opts, cache, false)
        {
            this.Options = opts;
        }

        protected override bool StartProcessingDirectory(DirectoryInfo directory, IEnumerable<FileInfo> files)
        {
            base.StartProcessingDirectory(directory, files);

            // Assign files to groups based on their filename key
            Dictionary<string, List<FileInfo>> matches = new Dictionary<string, List<FileInfo>>();
            foreach(var file in files)
            {
                var key = GenerateKey(file.Name);
                List<FileInfo> otherFiles = null;
                if (!matches.TryGetValue(key, out otherFiles))
                {
                    otherFiles = new List<FileInfo>();
                    matches[key] = otherFiles;
                }
                otherFiles.Add(file);
            }

            // For any key that matched multiple files, figure out what to do.
            foreach(var pair in matches)
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
            var files = new List<DuplicateScore>(from f in inputFiles select new DuplicateScore(f, ParseFile(f)));

            if (Opts.VerboseOutput)
            {
                Console.Write($"Potential match: {key}: ");
                foreach (var file in files)
                {
                    Console.Write(file.FileInfo.Name);
                }
                Console.WriteLine();
            }

            DateTimeOffset? captured = null;
            foreach(var file in files)
            {
                ComputeExtensionScore(file);
                if (!captured.HasValue)
                {
                    captured = file.MediaInfo.Taken;
                    file.IsDuplicate = true;
                }
                else if (captured.Value.Equals(file.MediaInfo.Taken))
                {
                    file.IsDuplicate = true;
                }
                else if (!file.MediaInfo.Taken.HasValue || string.IsNullOrEmpty(file.MediaInfo.CameraModel))
                {
                    file.IsDuplicate = true;
                    file.Score *= 0.1;
                }
            }
            ComputeFileSizeScore(files);

            var winner = files.FirstOrDefault();
            foreach(var file in files)
            {
                if (file.Score > winner.Score)
                {
                    winner = file;
                }
            }
            
            WriteDuplicateScore(key, files, winner);

            Console.WriteLine("Press any key to continue");
            Console.ReadLine();
        }

        private void WriteDuplicateScore(string key, List<DuplicateScore> files, DuplicateScore winner)
        {
            Console.WriteLine();
            Console.WriteLine($"Duplicate evaluation for {key}");
            Console.WriteLine($" -- Winner: {winner.FileInfo.Name}");
            Console.WriteLine();
            Console.WriteLine("    Name        \tSize\tScore\tDupe\tDateTaken\tModel");
            foreach (var file in files)
            {
                if (file == winner)
                {
                    Console.Write(" >> ");
                } else
                {
                    Console.Write("    ");
                }
                Console.WriteLine($"{file.FileInfo.Name}\t{file.FileInfo.Length}\t{(int)(file.Score * 100)}\t{(file.IsDuplicate ? "Y" : "N")}\t{file.MediaInfo.Taken}\t{file.MediaInfo.CameraModel}");
            }
        }

        private void ComputeFileSizeScore(List<DuplicateScore> files)
        {
            double meanFileSize = (from f in files select f.FileInfo.Length).Average();
            double meanFileNameLength = (from f in files select f.FileInfo.Name.Length).Average();
            foreach(var file in files)
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

            switch(file.FileInfo.Extension.ToLower())
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


            return true;
        }

        private class DuplicateScore
        {
            public FileInfo FileInfo { get; set; }
            public MediaInfo MediaInfo { get; set; }
            public double Score { get; set; }
            public bool IsDuplicate { get; set; }

            public DuplicateScore(FileInfo file, MediaInfo info)
            {
                FileInfo = file;
                MediaInfo = info;
                Score = 1;
                IsDuplicate = false;
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

        private void CheckForDuplicateFilenames()
        {

        }


        protected override void EndProcessingFilesInDirectory(DirectoryInfo directory)
        {
            base.EndProcessingFilesInDirectory(directory);

        }

    }
}

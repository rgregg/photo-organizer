using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    internal class DirectoryAndFileEnumerator<T>
    {
        protected CommonCommandLineOptions Opts { get; private set; }
        protected ParsedFileCache Cache { get; private set; }
        protected bool supportsParallel;

        public DirectoryAndFileEnumerator(CommonCommandLineOptions opts, ParsedFileCache cache, bool allowsParallel = true)
        {
            this.Opts = opts;
            this.Cache = cache;
            this.supportsParallel = allowsParallel;
        }

        public void ProcessSourceFolder(DirectoryInfo source)
        {
            var files = source.EnumerateFiles();
            if (StartProcessingDirectory(source, files))
            {
                if (supportsParallel && Opts.RunInParallel)
                {
                    Parallel.ForEach(files, file => ProcessFileParallel(file));
                }
                else
                {
                    T sharedState = default(T);
                    foreach (var file in files)
                    {
                        ProcessFile(file, ref sharedState);
                    }
                }
            }
            EndProcessingFilesInDirectory(source);

            if (Opts.Recursive)
            {
                var folders = source.EnumerateDirectories();
                if (supportsParallel && Opts.RunInParallel)
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

        protected virtual bool StartProcessingDirectory(DirectoryInfo directory, IEnumerable<FileInfo> files)
        {
            if (Opts.VerboseOutput)
            {
                Console.WriteLine("Process source folder {0} with {1} file(s).", directory.FullName, files.Count());
            }

            return true;
        }

        protected virtual void EndProcessingFilesInDirectory(DirectoryInfo directory)
        { 

        }

        protected virtual MediaInfo ParseFile(FileInfo fileInfo)
        {
            MediaInfo info = null;
            bool fromCache = false;
            if (Opts.CacheFileInfo)
            {
                MediaInfo cachedData;
                if (Cache.CacheLookup(fileInfo, out cachedData))
                {
                    info = cachedData;
                    fromCache = true;
                }
            }

            if (null == info)
            {
                info = MediaInfoFactory.GetMediaInfo(fileInfo, Opts.DataParser);
            }

            if (Opts.CacheFileInfo && !fromCache)
            {
                Cache.Add(info);
            }
            
            return info;
        }

        protected virtual void ProcessFileParallel(FileInfo file)
        {
            T value = default(T);
            ProcessFile(file, ref value);
        }

        protected virtual void ProcessFile(FileInfo file, ref T suggestion)
        {
            throw new NotImplementedException();
        }

        protected void VerboseLog(string message)
        {
            if (Opts.VerboseOutput)
            {
                Console.WriteLine(message);
            }
        }

        protected bool FilesAreIdentifical(FileInfo source, FileInfo destination)
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

        protected string GetChecksum(FileInfo file)
        {
            int buffer_size = 1024 * 1024;
            using (BufferedStream stream = new BufferedStream(file.OpenRead(), buffer_size))
            {
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] checksum = md5.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer.LocalFileSystem
{
    class LocalFile : IFile
    {
        private readonly FileInfo _file;
        public LocalFile(FileInfo file)
        {
            _file = file;
            if (file.Exists)
            {
                LoadFileDetails();
            }
        }

        public long Length
        {
            get { return _file.Length; }
        }

        public string ComputeSha1Hash()
        {
            using (FileStream fs = new FileStream(_file.FullName, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }
                    return formatted.ToString();
                }
            }
        }

        public DetailFileInfo.PerceivedFileType PerceivedType
        {
            get; private set;
        }

        public DateTimeOffset? DateTaken
        {
            get; private set;
        }

        public string CameraMake
        {
            get; private set;
        }

        public string CameraModel
        {
            get; private set;
        }

        public string FullName
        {
            get { return _file.FullName; }
        }

        public string Name
        {
            get { return _file.Name; }
        }

        public IDirectory CurrentDirectory
        {
            get { return new LocalDirectory(_file.Directory); }
        }

        private void LoadFileDetails()
        {
            var attributes = new DetailFileInfo.FileAttributes[] { DetailFileInfo.FileAttributes.PerceivedType, DetailFileInfo.FileAttributes.DateTaken, 
                    DetailFileInfo.FileAttributes.CameraMaker, DetailFileInfo.FileAttributes.CameraModel };

            DetailFileInfo.CFileInfo info = new DetailFileInfo.CFileInfo(_file.FullName, attributes);
            PerceivedType = info.PerceivedType;
            DateTaken = info.DateTaken;
            CameraMake = info.CameraMake;
            CameraModel = info.CameraModel;

        }

        /// <summary>
        /// Generates a unique filename for the current file in the given target directory
        /// by appending a number to the end of the filename
        /// 
        /// foo.jpg -> foo 1.jpg
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        private async Task<string> GenerateUniqueFilenameAsync(IDirectory targetDirectory)
        {
            int incrementValue = 1;
            string rootFilename = Path.GetFileNameWithoutExtension(this.Name);
            string extension = Path.GetExtension(this.Name);

            IFile newTargetFile = null;
            for(int i=1; i<100; i++)
            {
                var newFilename = string.Format("{0} {1}.{2}", rootFilename, incrementValue, extension);
                newTargetFile = await this.CurrentDirectory.GetFileAsync(newFilename);
                if (!newTargetFile.Exists)
                    break;
            }

            if (!newTargetFile.Exists)
                return newTargetFile.Name;

            throw new InvalidOperationException("Couldn't find a unique filename.");
        }


        public async Task CopyToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFilename = null)
        {
            if (targetDirectory.GetType() != this.GetType())
                throw new ArgumentException("targetDirectory must be of same type");

            bool tryAgain = false;
            try
            {
                var targetPath = Path.Combine(targetDirectory.FullName, newFilename ?? _file.Name);
                _file.CopyTo(targetPath, fileExistsBehavior == ExistingFileMode.Overwrite);
            }
            catch (IOException ioex)
            {
                switch (ioex.HResult)
                {
                    case -2147024816:   // File already exists
                    case -2147024713:
                        tryAgain = true;
                        break;
                    default:
                        Console.WriteLine("File skipped (IOException: {0}): {1}\r\n{2}", ioex.HResult, _file.Name, ioex.Message);
                        break;
                }
            }

            if (tryAgain)
            {
                switch (fileExistsBehavior)
                {
                    case ExistingFileMode.Ignore:
                        Console.WriteLine("Skipping file (already exists): {0}", this.FullName);
                        break;
                    case ExistingFileMode.Rename:
                        var newfilename = await GenerateUniqueFilenameAsync(targetDirectory);
                        await this.CopyToAsync(targetDirectory, fileExistsBehavior, newfilename);
                        break;
                    case ExistingFileMode.DeleteSourceFileWhenIdentical:
                        Console.WriteLine("Deleting source file (target already exists): {0}", this.FullName);
                        var destinationFile = await targetDirectory.GetFileAsync(this.Name);
                        if (this.IsFileIdentical(destinationFile))
                        {
                            await this.DeleteAsync();
                        }
                        break;
                }
            }
        }

        public async Task MoveToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFilename = null)
        {
            if (targetDirectory.GetType() != this.GetType())
                throw new ArgumentException("targetDirectory must be of same type");

            bool tryAgain = false;
            try
            {
                var targetPath = Path.Combine(targetDirectory.FullName, newFilename ?? _file.Name);
                _file.MoveTo(targetPath);
            }
            catch (IOException ioex)
            {
                switch (ioex.HResult)
                {
                    case -2147024816:   // File already exists
                    case -2147024713:
                        tryAgain = true;
                        break;
                    default:
                        Console.WriteLine("File skipped (IOException: {0}): {1}\r\n{2}", ioex.HResult, _file.Name, ioex.Message);
                        break;
                }
            }

            if (tryAgain)
            {
                switch (fileExistsBehavior)
                {
                    case ExistingFileMode.Ignore:
                        Console.WriteLine("Skipping file (already exists): {0}", this.FullName);
                        break;
                    case ExistingFileMode.Rename:
                        var newfilename = await GenerateUniqueFilenameAsync(targetDirectory);
                        await this.MoveToAsync(targetDirectory, fileExistsBehavior, newfilename);
                        break;
                    case ExistingFileMode.DeleteSourceFileWhenIdentical:
                        Console.WriteLine("Deleting source file (target already exists): {0}", this.FullName);
                        var destinationFile = await targetDirectory.GetFileAsync(this.Name);
                        if (this.IsFileIdentical(destinationFile))
                        {
                            await this.DeleteAsync();
                        }
                        break;
                }
            }
        }

        public async Task DeleteAsync()
        {
            _file.Delete();
        }

        public DateTimeOffset DateTimeLastModified
        {
            get { return _file.LastWriteTimeUtc; }
        }

        public bool Exists { get { return _file.Exists; } }


        public async Task CopyToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort)
        {
            throw new NotImplementedException();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public abstract class BaseFileInfo : IMediaInfo
    {
        protected System.IO.FileInfo sourceFile;
        public BaseFileInfo(System.IO.FileInfo file)
        {
            sourceFile = file ?? throw new ArgumentNullException("file");
            ParseFile();
        }

        protected abstract void ParseFile();

        public string Filename { get { return sourceFile.Name; } }
        public string Path { get { return sourceFile.FullName; } }
        public DateTimeOffset Created
        {
            get
            {
                return new DateTimeOffset(sourceFile.CreationTimeUtc);
            }
        }
        public DateTimeOffset LastModified
        {
            get
            {
                return new DateTimeOffset(sourceFile.LastWriteTimeUtc);
            }
        }
        public DateTimeOffset? Taken { get; protected set; }
        public string CameraMake { get; protected set; }
        public string CameraModel { get; protected set; }
        public MediaType Type { get; protected set; }

        protected bool IsVideoFile()
        {
            switch (sourceFile.Extension.ToLowerInvariant())
            {
                case ".mp4":
                case ".mpg":
                case ".wmv":
                case ".mov":
                    return true;
                default:
                    return false;
            }
        }
    }
}

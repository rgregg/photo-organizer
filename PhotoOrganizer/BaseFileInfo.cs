using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public abstract class BaseFileInfo : MediaInfo
    {
        protected System.IO.FileInfo sourceFile;
        public BaseFileInfo(System.IO.FileInfo file)
        {
            sourceFile = file ?? throw new ArgumentNullException("file");
            ParseFile();
        }

        protected abstract void ParseFile();

        public override string Filename
        {
            get { return sourceFile.Name; }
            set { throw new NotSupportedException(); }
        }
        public override string FullPath { 
            get { return sourceFile.FullName; } 
            set { throw new NotSupportedException(); }
        }
        public override DateTimeOffset Created
        {
            get { return new DateTimeOffset(sourceFile.CreationTimeUtc); }
            set { throw new NotSupportedException(); }
        }
        public override long Size
        {
            get { return sourceFile.Length; }
            set { throw new NotSupportedException(); }
        }
        public override DateTimeOffset LastModified
        {
            get { return new DateTimeOffset(sourceFile.LastWriteTimeUtc); }
            set { throw new NotSupportedException(); }
        }

        protected bool IsVideoFile()
        {
            var mediaTypes = from ext in MediaTypes.VideoFormatExtensions.Split(',')
                                  select "." + ext.ToLowerInvariant();

            if (mediaTypes.Contains(sourceFile.Extension.ToLowerInvariant())) {
                return true;
            }

            return false;
        }

        protected bool IsPhotoFile()
        {
            var mediaTypes = from ext in MediaTypes.PhotoFormatExtensions.Split(',')
                             select "." + ext.ToLowerInvariant();

            if (mediaTypes.Contains(sourceFile.Extension.ToLowerInvariant()))
            {
                return true;
            }

            return false;
        }
    }
}

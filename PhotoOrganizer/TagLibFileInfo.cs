using System;

namespace PhotoOrganizer
{
    public class TagLibFileInfo : IMediaInfo
    {
        private System.IO.FileInfo sourceFile;

        public TagLibFileInfo(System.IO.FileInfo file)
        {
            if (null == file)
                throw new ArgumentNullException("file");
            
            this.sourceFile = file;
            ParseFile();
        }

        public string Filename {get { return sourceFile.Name; }}
        public string Path {get { return sourceFile.FullName; }}

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

        public DateTimeOffset? Taken {get; private set;}
        public string CameraMake {get; private set;}
        public string CameraModel {get; private set;}
        public MediaType Type {get; private set;}

        private void ParseFile()
        {
            switch (sourceFile.Extension.ToLowerInvariant())
            {
                case ".mp4":
                case ".mpg":
                case ".wmv":
                case ".mov":
                    Type = MediaType.Video;
                    break;
            }

            TagLib.File file = null;
            try
            {
                file = TagLib.File.Create(sourceFile.FullName);
            }
            //catch (TagLib.UnsupportedFormatException)
            //{
            //    Type = MediaType.Unknown;
            //    return;
            //}
            //catch (TagLib.CorruptFileException)
            //{
            //    Type = MediaType.Unknown;
            //    return;
            //}
            catch (Exception)
            {
                Type = MediaType.Unknown;
                return;
            }

            var image = file as TagLib.Image.File;
            if (null != image)
            {
                Type = MediaType.Image;
                CameraMake = image.ImageTag.Make;
                CameraModel = image.ImageTag.Model;
                Taken = image.ImageTag.DateTime;
            }
        }
    }
}


using System;

namespace PhotoOrganizer
{
    public class TagLibFileInfo : BaseFileInfo
    {
        public TagLibFileInfo(System.IO.FileInfo file) : base(file)
        {
        }
        protected override void ParseFile()
        {
            if (IsVideoFile())
            {
                Type = MediaType.Video;
            }
            else if (IsPhotoFile())
            {
                Type = MediaType.Image;
            }

            TagLib.File file = null;
            try
            {
                file = TagLib.File.Create(sourceFile.FullName);
            }
            catch (Exception)
            {
                Type = MediaType.Unknown;
                return;
            }

            if (file is TagLib.Image.File image)
            {
                Type = MediaType.Image;
                CameraMake = image.ImageTag.Make;
                CameraModel = image.ImageTag.Model;
                Taken = image.ImageTag.DateTime;
            }
        }
    }
}


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
            else
            {
                Type = MediaType.Unknown;
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

            var imageTag = file.Tag as TagLib.Image.CombinedImageTag;
            if (null != imageTag)
            {
                Type = MediaType.Image;
                CameraMake = imageTag.Make;
                CameraModel = imageTag.Model;
                Taken = imageTag.DateTime;
                return;
            }

            if (file.Properties.VideoWidth > 0)
            {
                Type = MediaType.Video;

            }
            
        }
    }
}


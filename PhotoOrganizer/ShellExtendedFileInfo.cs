using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace PhotoOrganizer
{

   


    public class ShellExtendedFileInfo : BaseFileInfo
    {
        public ShellExtendedFileInfo(System.IO.FileInfo file) : base(file)
        {
        }


        protected override void ParseFile()
        {
            if (IsVideoFile())
            {
                Type = MediaType.Video;
            }
            else if( IsPhotoFile())
            {
                Type = MediaType.Image;
            }
            else
            {
                Type = MediaType.Unknown;
            }

            try
            {
                var file = ShellFile.FromFilePath(sourceFile.FullName);
                Taken = file.Properties.System.Photo.DateTaken.Value ?? file.Properties.System.Media.DateEncoded.Value;
                CameraMake = file.Properties.System.Photo.CameraManufacturer.Value;
                CameraModel = file.Properties.System.Photo.CameraModel.Value;
                if (Type == MediaType.Unknown && !string.IsNullOrEmpty(CameraMake))
                {
                    Type = MediaType.Image;
                }
            }
            catch (Exception ex)
            {
                Type = MediaType.Unknown;
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

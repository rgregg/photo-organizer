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

            try
            {
                var file = ShellFile.FromFilePath(sourceFile.FullName);
                Taken = file.Properties.System.Photo.DateTaken.Value;
                CameraMake = file.Properties.System.Photo.CameraManufacturer.Value;
                CameraModel = file.Properties.System.Photo.CameraModel.Value;
                if (Taken != null) {
                    Type = MediaType.Image;
                }
                else
                {
                    Type = MediaType.Unknown;
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

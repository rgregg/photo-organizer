using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

#if Windows
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
#endif


namespace MediaOrganizerConsoleApp
{
    public class MediaMetadataReader
    {
        public static MediaMetadata ParseFile(FileInfo sourceFile, ILogWriter logger)
        {
            if (null == logger) throw new ArgumentNullException("logger");

            logger.WriteLog($"Metadata parser: Reading {sourceFile.Name} metadata with Shell32.", true);

            var results = new MediaMetadata();
#if Windows
            try
            {
                var file = ShellFile.FromFilePath(sourceFile.FullName);
                results.DateCaptured = file.Properties.System.Photo.DateTaken.Value ?? file.Properties.System.Media.DateEncoded.Value;
                results.CameraMake = file.Properties.System.Photo.CameraManufacturer.Value;
                results.CameraModel = file.Properties.System.Photo.CameraModel.Value;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Unable to read metadata from {sourceFile.Name}: {ex.Message}", true);
            }
#elif Linux || OSX
            // TBD
#endif

            return results;
        }
    }
}

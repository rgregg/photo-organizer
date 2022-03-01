using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MediaOrganizerConsoleApp.FormatConversion
{
    public class ImageMagick : AppInvoker
    {
        private static readonly string ImageMagickBinaryName = "magick.exe";
        

        static ImageMagick()
        {
            // Set the binary names based on the OS
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ImageMagickBinaryName = "magick";
                
            }
        }

        public ImageMagick(ILogWriter logger)
        {
            Logger = logger;
            CopyMetadata = true;
            Resize = false;
            OutputFormat = "jpg";
            OutputQuality = 80;
            Geometry = null;
            Overwrite = false;
        }

        public GeometrySettings Geometry { get; set; }
        public bool CopyMetadata { get; set; }
        public string OutputFormat { get; set; }
        public int OutputQuality { get; set; }
        private ILogWriter Logger { get; set; }
        public bool Resize { get; set; }
        public bool Overwrite { get; set; }

        public void ConvertFormat(FileInfo source, string destination)
        {
            var tempDestination = Path.GetTempFileName();
            try
            {
                ConvertMediaFile(source.FullName, tempDestination);

                if (CopyMetadata)
                {
                    ExifTool.CopyExifMetadata(source.FullName, tempDestination, Logger);
                }

                var finalDestinationFile = new FileInfo(destination);
                var finalDestinationDirectory = finalDestinationFile.Directory;
                if (!finalDestinationDirectory.Exists)
                {
                    finalDestinationDirectory.Create();
                }

                File.Copy(tempDestination, destination, Overwrite);
            }
            catch (MediaConversionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error converting file format: {ex.Message}.", false);
            }
            finally
            {
                File.Delete(tempDestination);
            }
        }

        protected void ConvertMediaFile(string source, string destination)
        {
            if (Resize && Geometry == null)
                throw new NotSupportedException("Geometry must be provided if Resize is true.");
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("source requires a value.");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("destination requires a value.");

            if (!File.Exists(source))
                throw new ArgumentException("source does not exist.");
            
            List<string> cmdParams = new List<string>() { source };
            if (Resize)
            {
                cmdParams.Add("-resize");
                cmdParams.Add(Geometry.CommandLineParameter);
            }
            if (OutputQuality > 0)
            {
                cmdParams.Add("-quality");
                cmdParams.Add(OutputQuality.ToString());
            }
            cmdParams.Add($"{OutputFormat}:{destination}");

            try
            {
                var result = RunProcess(ImageMagickBinaryName, cmdParams, Logger);
                if (result.ExitCode != 0)
                {
                    throw new MediaConversionException($"Unable to convert media file, ImageMagick returned an error code: {result.ExitCode}.") 
                    {
                        StandardOutput = result.StandardOutput, 
                        StandardError = result.StandardError,
                        ExitCode = result.ExitCode,
                        CommandLine = result.CommandLine,
                    };
                }
            }
            catch (MediaConversionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MediaConversionException($"Unable to convert media file, an unexpected error occured: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verify that a compatible version of ImageMagick is installed
        /// </summary>
        /// <returns></returns>
        public static bool IsToolInstalled(ILogWriter logger)
        {
            try
            {
                var results = RunProcess(ImageMagickBinaryName, new string[] { "--version" }, logger);

                using (var reader = new StringReader(results.StandardOutput))
                {
                    var version = reader.ReadLine();
                    logger.WriteLog($"ImageMagick {version}", false);
                }

                return ExifTool.IsToolInstalled(logger);
            }
            catch (Exception ex)
            {
                logger.WriteLog($"ImageMagick is not installed: {ex.Message}", false);
                return false;
            }
        }

        public class GeometrySettings
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public ResizeOptions Mode { get; set; }

            public string CommandLineParameter
            {
                get
                {
                    StringBuilder output = new StringBuilder($"\"{Width}x{Height}");
                    switch (Mode)
                    {
                        case ResizeOptions.Percentage:
                            output.Append("%");
                            break;
                        case ResizeOptions.MaximumSizeAspectPreserved:
                        case ResizeOptions.Default:
                            break;
                        case ResizeOptions.MinimumSizeAspectPreserved:
                            output.Append("^");
                            break;
                        case ResizeOptions.ForceSize:
                            output.Append("!");
                            break;
                        case ResizeOptions.ShrinkAspectPreserved:
                            output.Append(">");
                            break;
                        case ResizeOptions.EnlargeAspectPreserved:
                            output.Append("<");
                            break;
                    }
                    output.Append("\"");
                    return output.ToString();
                }
            }
        }

        public enum ResizeOptions
        {
            Default,
            Percentage,                  // widthxheight%
            MaximumSizeAspectPreserved,  // widthxheight
            MinimumSizeAspectPreserved,  // widthxheight^
            ForceSize,                   // widthxheight!
            ShrinkAspectPreserved,       // widthxheight>
            EnlargeAspectPreserved,      // widthxheight<
        }

    }
}

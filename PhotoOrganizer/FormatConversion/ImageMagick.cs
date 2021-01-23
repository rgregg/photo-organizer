using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PhotoOrganizer.FormatConversion
{
    public class ImageMagick
    {
        private static readonly string ImageMagickBinaryName = "magick.exe";
        private static readonly string ExifToolBinary = "exiftool.exe";
        private static readonly char ArgumentEscapeChar = '\"';

        static ImageMagick()
        {
            // TODO: Set the binary names based on the OS

        }

        public ImageMagick(ILogWriter logger)
        {
            Logger = logger;
            CopyMetadata = true;
            OutputFormat = "jpg";
            OutputQuality = 100;
            DefaultGeometry = new Geometry { Width = 2048, Height = 2048, Mode = ResizeOptions.Default };
        }

        public Geometry DefaultGeometry { get; set; }
        public bool CopyMetadata { get; set; }
        public string OutputFormat { get; set; }
        public int OutputQuality { get; set; }
        private ILogWriter Logger { get; set; }

        public bool ConvertFormat(FileInfo source, string destination)
        {
            var tempDestination = Path.GetTempFileName();

            try
            {
                var result = ConvertImage(source.FullName, DefaultGeometry, destination);
                if (!result)
                {
                    Logger.WriteLog($"Unable to convert file {source.Name} to desired format.", false);
                    return false;
                }

                if (CopyMetadata)
                {
                    result = CopyExifMetadata(source.FullName, tempDestination);
                    if (!result)
                    {
                        Logger.WriteLog($"Unable to copy metadata from {source.Name} to output file.", false);
                        return false;
                    }
                }

                File.Copy(source.FullName, destination);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error converting file format: {ex.Message}.", false);
                return false;
            }
            finally
            {
                File.Delete(tempDestination);
            }
        }

        public bool ConvertImage(string source, Geometry geo, string destination)
        {
            List<string> cmdParams = new List<string> 
            {
                source,
                "-resize",
                geo.CommandLineParameter,
                "-quality",
                OutputQuality.ToString(),
                $"{OutputFormat}:{destination}" 
            };

            int result = RunProcess(ImageMagickBinaryName, cmdParams, Logger);
            return (result == 0);
        }

        public bool CopyExifMetadata(string source, string destination)
        {
            List<string> cmdParams = new List<string>
            {
                "-tagsfromfile",
                source,
                "-all:all",
                destination
            };

            int result = RunProcess(ExifToolBinary, cmdParams, Logger);
            return (result == 0);
        }

        private static int RunProcess(string binaryName, IEnumerable<string> args, ILogWriter logger)
        {
            var proc = new Process {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = binaryName,
                    Arguments = ConvertToArgs(args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            logger.WriteLog($"Executing {proc.StartInfo.FileName} {proc.StartInfo.Arguments}...", true);

            try
            {
                proc.Start();
                proc.WaitForExit();
                
                // Read the console output
                string result = proc.StandardOutput.ReadToEnd();

                return proc.ExitCode;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Error executing process: {ex.Message}", true);
                throw;
            }
        }

        private static string ConvertToArgs(IEnumerable<string> input)
        {
            StringBuilder output = new StringBuilder();
            foreach(string arg in input)
            {
                if (arg.Contains(' ')) {
                    output.Append(ArgumentEscapeChar);
                    output.Append(arg);
                    output.Append(ArgumentEscapeChar);
                }
                else
                {
                    output.Append(arg);
                }
            }
            return output.ToString();
        }

        public class Geometry
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public ResizeOptions Mode { get; set; }

            public string CommandLineParameter
            {
                get
                {
                    StringBuilder output = new StringBuilder($"'{Width}x{Height}");
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
                    output.Append("'");
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

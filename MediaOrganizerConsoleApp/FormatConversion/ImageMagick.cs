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
            Resize = false;
            OutputFormat = "jpg";
            OutputQuality = 80;
            Geometry = null;
        }

        public GeometrySettings Geometry { get; set; }
        public bool CopyMetadata { get; set; }
        public string OutputFormat { get; set; }
        public int OutputQuality { get; set; }
        private ILogWriter Logger { get; set; }
        public bool Resize { get; set; }

        public void ConvertFormat(FileInfo source, string destination)
        {
            var tempDestination = Path.GetTempFileName();
            try
            {
                ConvertMediaFile(source.FullName, tempDestination);

                if (CopyMetadata)
                {
                    CopyExifMetadata(source.FullName, tempDestination);
                }

                var finalDestinationFile = new FileInfo(destination);
                var finalDestinationDirectory = finalDestinationFile.Directory;
                if (!finalDestinationDirectory.Exists)
                {
                    finalDestinationDirectory.Create();
                }

                File.Copy(tempDestination, destination);
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
                        ExitCode = result.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                throw new MediaConversionException($"Unable to convert media file, an unexpected error occured: {ex.Message}", ex);
            }
        }

        protected void CopyExifMetadata(string source, string destination)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("source cannot be empty");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("destination cannot be empty.");
            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file '{source}' does not exist.");
            if (!File.Exists(destination))
                throw new FileNotFoundException($"Destination file '{destination}' does not exist.");


            List<string> cmdParams = new List<string>
            {
                "-tagsfromfile",
                source,
                "-all:all",
                destination
            };


            try
            {
                var result = RunProcess(ExifToolBinary, cmdParams, Logger);
                if (result.ExitCode != 0)
                {
                    throw new MediaConversionException($"Unable to copy exif metadata to destination file. exiftool returned an error: {result.ExitCode}")
                    {
                        StandardError = result.StandardError,
                        StandardOutput = result.StandardOutput,
                        ExitCode = result.ExitCode
                    };
                }
            }
            catch (MediaConversionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MediaConversionException($"Unable to copy exif metadata to the destination file: {ex.Message}", ex);
            }
        }

        private class RunProcessResults
        {
            public RunProcessResults() { }

            public RunProcessResults(Process proc)
            {
                if (!proc.HasExited)
                {
                    throw new ArgumentException("Cannot create a RunProcessResults instance on a process that is still running.");
                }

                ExitCode = proc.ExitCode;
                StandardOutput = proc.StandardOutput.ReadToEnd();
                StandardError = proc.StandardError.ReadToEnd();
            }

            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
        }

        private static RunProcessResults RunProcess(string binaryName, IEnumerable<string> args, ILogWriter logger)
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

                return new RunProcessResults(proc);
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
                output.Append(' ');
            }
            return output.ToString();
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

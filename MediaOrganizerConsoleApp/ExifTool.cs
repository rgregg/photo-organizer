using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;


namespace MediaOrganizerConsoleApp
{
    public class ExifTool : AppInvoker
    {
        private static readonly string ExifToolBinary = "exiftool.exe";

        static ExifTool()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ExifToolBinary = "exiftool";
            }
        }

        public static bool IsToolInstalled(ILogWriter logger)
        {
             try
            {
                var results = RunProcess(ExifToolBinary, new string[] { "-ver" }, logger);
                using (var reader = new StringReader(results.StandardOutput))
                {
                    var version = reader.ReadLine();
                    logger.WriteLog($"exiftool version: {version}", false);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"ExifTool is not available: {ex.Message}", false);
                return false;
            }
}

        public static MediaMetadata ParseFile(FileInfo sourceFile, ILogWriter logger)
        {
            if (null == logger) 
                throw new ArgumentNullException("logger");

            logger.WriteLog($"Metadata parser: Reading {sourceFile.Name} metadata with Shell32.", true);

            var args = new List<string> 
            {
                "-s",
                "-DateTimeOriginal",
                "-OffsetTimeOriginal",
                "-Make",
                "-Model",
                "-FileType",
                "-HandlerType",
                "-ExifImageWidth",
                "-ExifImageHeight",
                "-json",
                sourceFile.Name
            };

            try
            {
                var results = RunProcess(ExifToolBinary, args, logger, workingPath: sourceFile.DirectoryName);
                if (results.ExitCode != 0)
                {
                    throw new AppInvokerException($"Invalid exit code from exiftool: {results.ExitCode}.");
                }

                var result = JsonConvert.DeserializeObject<List<ExifToolOutput>>(results.StandardOutput).FirstOrDefault();
                if (null != result)
                {
                    var captureDateString = $"{result.DateTimeOriginal} {result.OffsetTimeOriginal}";

                    DateTimeOffset? captureDate = null;
                    if (!DateTimeOffset.TryParse(captureDateString.Trim(), out DateTimeOffset parseResult))
                    {
                        // 2019:01:07 16:33:35
                        if (DateTimeOffset.TryParseExact(captureDateString.Trim(), new string[] { "yyyy:MM:dd HH:mm:ss", "yyyy:MM:dd HH:mm:ss zzz"}, null, System.Globalization.DateTimeStyles.AssumeLocal, out parseResult))
                        {
                            captureDate = parseResult;
                        }
                        else 
                        {
                            logger.WriteLog($"Unable to parse CaptureDate: {captureDateString}", false);
                            captureDate = null;
                        }
                    }
                    else
                    {
                        captureDate = parseResult;
                    }

                    return new MediaMetadata
                    {
                        CameraMake = result.Make,
                        CameraModel = result.Model,
                        DateCaptured = captureDate,
                        Width = result.ExifImageWidth,
                        Height = result.ExifImageHeight
                    };
                }
            }
            catch (AppInvokerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.WriteLog($"Error: unable to read metadata from {sourceFile.FullName}: {ex.Message}", false);
            }

            return new MediaMetadata();
        }

        public static void CopyExifMetadata(string source, string destination, ILogWriter logger)
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
                var result = RunProcess(ExifToolBinary, cmdParams, logger);
                if (result.ExitCode != 0)
                {
                    throw new AppInvokerException($"Unable to copy exif metadata to destination file. exiftool returned an error: {result.ExitCode}")
                    {
                        StandardError = result.StandardError,
                        StandardOutput = result.StandardOutput,
                        ExitCode = result.ExitCode,
                        CommandLine = result.CommandLine,
                    };
                }
            }
            catch (AppInvokerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppInvokerException($"Unable to copy exif metadata to the destination file: {ex.Message}", ex);
            }
        }

        class ExifToolOutput {
            public string DateTimeOriginal { get; set; }
            public string OffsetTimeOriginal { get; set; }
            public string Make { get; set; }
            public string Model { get; set; }
            public string FileType { get; set; }
            public string HandlerType { get; set; }
            public int ExifImageWidth { get; set; }
            public int ExifImageHeight { get; set; }
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MediaOrganizerConsoleApp
{
    public class FileTypeRecognizer
    {
        // Useful list of file signatures here: https://en.wikipedia.org/wiki/List_of_file_signatures
        // ftyp values for various formats here: https://www.ftyps.com/

        private static List<FormatSignature> Formats = new List<FormatSignature>()
        {
            // Image formats
            new FormatSignature(MediaType.Image, BinaryFormat.Jpeg, new string[] { ".jpg", ".jpeg" }, 
                new ByteRangeMatch("FF D8 FF E0 ?? ?? 4A 46 49 46 00"),
                new ByteRangeMatch("FF D8 FF DB"),
                new ByteRangeMatch("FF D8 FF E1 ?? ?? 45 78 69 66 00"),
                new ByteRangeMatch("FF D8 FF EE")),
            new FormatSignature(MediaType.Image, BinaryFormat.Png, ".png", 
                new ByteRangeMatch(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)),
            new FormatSignature(MediaType.Image, BinaryFormat.Heic, ".heic", 
                new ByteRangeMatch(ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63, 0x00 )),
            new FormatSignature(MediaType.Image, BinaryFormat.CanonRawCr2, ".cr2", 
                new ByteRangeMatch(0x49, 0x49, 0x2A, 0x00, 0x10, 0x00, 0x00, 0x00, 0x43, 0x52, 0x02, 0x00 )),
            new FormatSignature(MediaType.Image, BinaryFormat.Gif, ".gif",
                new ByteRangeMatch(0x47, 0x49, 0x46, 0x38, 0x37, 0x61),
                new ByteRangeMatch(0x47, 0x49, 0x46, 0x38, 0x39, 0x61)),
            new FormatSignature(MediaType.Image, BinaryFormat.Dng, ".dng",
                new ByteRangeMatch(0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, ByteRangeMatch.Wildcard, 0x00, 0xfe, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00),
                new ByteRangeMatch(0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00)),
            new FormatSignature(MediaType.Image, BinaryFormat.Bitmap, ".bmp"),
            new FormatSignature(MediaType.Image, BinaryFormat.Tiff, new string[] { ".tif", ".tiff" }),

            // Video formats
            new FormatSignature(MediaType.Video, BinaryFormat.Avi, ".avi",
                new ByteRangeMatch(0x52, 0x49, 0x46, 0x46, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, 0x41, 0x56, 0x49, 0x20)),
            new FormatSignature(MediaType.Video, BinaryFormat.Mp4, ".mp4",
                new TextRangeMatch(4, "ftypmp41"),
                new TextRangeMatch(4, "ftypmp42"),
                new TextRangeMatch(4, "ftypavc1"),
                new TextRangeMatch(4, "ftypisom")),
            new FormatSignature(MediaType.Video, BinaryFormat.QuickTime, ".mov",
                new ByteRangeMatch(ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, ByteRangeMatch.Wildcard, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x71, 0x74, 0x20, 0x20),
                new TextRangeMatch(4, "ftypqt")),
            new FormatSignature(MediaType.Video, BinaryFormat.Ogg, ".ogg"),
            new FormatSignature(MediaType.Video, BinaryFormat.Mpeg, new string[] { ".mpg", ".mpeg", ".mpe", ".mp2" }),
            new FormatSignature(MediaType.Video, BinaryFormat.ProtectedMp4, new string[] { ".m4p", ".m4v" }),
            new FormatSignature(MediaType.Video, BinaryFormat.WebM, ".webm"),
            new FormatSignature(MediaType.Video, BinaryFormat.WindowsMedia, ".wmv"),
            
            // Other data types
            new FormatSignature(MediaType.Metadata, BinaryFormat.Xmp, ".xmp",
                new TextRangeMatch(0, "<x:xmpmeta ")) { Excluded = true },
            new FormatSignature(MediaType.System, BinaryFormat.MacOsFinder, ".DS_Store") { Excluded = true }

        };

        static int S_MaxBytes = -1;
        public int MaxBytesToRead
        {
            get
            {
                if (S_MaxBytes > 0)
                {
                    return S_MaxBytes;
                }

                int maxValueFound = 0;
                foreach(var format in Formats)
                {
                    foreach(var header in format.Headers)
                    {
                        if (header.Length > maxValueFound)
                        {
                            maxValueFound = header.Length;
                        }
                    }
                }
                S_MaxBytes = maxValueFound;
                return S_MaxBytes;
            }
        }


        public bool UseDeepInspection { get; private set; }
        protected ILogWriter LogWriter { get; private set; }
        public FileTypeRecognizer(bool useDeepInspection, ILogWriter logWriter)
        {
            UseDeepInspection = useDeepInspection;
            LogWriter = LogWriter;
        }

        /** Evaluates the bindary file format for a file by observing the headers in the file **/
        public FormatSignature DetectSignature(FileInfo fileInfo)
        {
            IEnumerable<FormatSignature> matchingFormats = null;
            if (UseDeepInspection)
            {
                using (var file = File.OpenRead(fileInfo.FullName))
                {
                    byte[] fileHeader = new byte[MaxBytesToRead];
                    int bytesRead = file.Read(fileHeader, 0, fileHeader.Length);

                    matchingFormats = from f in Formats where f.HeaderMatches(fileHeader, bytesRead) select f;
                    if (matchingFormats.Any())
                    {
                        return matchingFormats.First();
                    }
                }
            }

            // Use the file extension to determine the signature
            var extension = fileInfo.Extension;
            matchingFormats = from f in Formats where f.ExtensionMatches(extension) select f;
            if (matchingFormats.Any())
            {
                return matchingFormats.First();
            }

            if (UseDeepInspection)
            {
                string header = ReadFileHeader(fileInfo);

                if (null != LogWriter)
                {
                    LogWriter.WriteLog($"File {fileInfo.FullName} of type {MediaType.Default} has header:\n{header}", false);
                }
            }

            return new FormatSignature(MediaType.Default, BinaryFormat.Unknown, fileInfo.Extension);
        }

        private string ReadFileHeader(FileInfo file)
        {
            byte[] headers = new byte[20];
            int bytesRead = 0;
            using (var stream = file.OpenRead())
            {
                bytesRead = stream.Read(headers, 0, headers.Length);
            }

            StringBuilder outputHex = new StringBuilder();
            StringBuilder outputString = new StringBuilder();
            for(int index = 0; index < bytesRead; index++)
            {
                outputHex.Append(headers[index].ToString("X2"));
                outputHex.Append(" ");

                outputString.Append(' ');
                if (headers[index] >= 32 && headers[index] < 128)
                {
                    outputString.Append(Convert.ToChar(headers[index]));
                }
                else
                {
                    outputString.Append(' ');
                }
                outputString.Append(" ");
            }

            return $"Hex: {outputHex}\nChr: {outputString}";
        }

        /// <summary>
        /// Return the expected file extension for a file format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public string FileFormatExpectedExtension(BinaryFormat format)
        {
            var extension = from f in Formats
                            where f.Format == format
                            select f.Extensions.First();
            return extension.FirstOrDefault();
        }
    }

    public enum BinaryFormat
    {
        Unknown = 0,
        Jpeg,
        Png,
        CanonRawCr2,
        Gif,
        Avi,
        Mp4,
        Heic,
        QuickTime,
        Dng,
        Xmp,
        Bitmap,
        Ogg,
        Mpeg,
        ProtectedMp4,
        WebM,
        WindowsMedia,
        Tiff,
        MacOsFinder
    }

}

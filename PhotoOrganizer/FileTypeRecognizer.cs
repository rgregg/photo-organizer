using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PhotoOrganizer
{
    public class FileTypeRecognizer
    {
        private static List<FormatMagicData> Formats = new List<FormatMagicData>()
        {
            new FormatMagicData(MediaType.Image, FileBinaryFormat.Jpeg, ".jpg", 
                new byte?[] { 0xFF, 0xD8, 0xFF, 0xE0, null, null, 0x4A, 0x46, 0x49, 0x46, 0x00 },
                new byte?[] { 0xFF, 0xD8, 0xFF, 0xDB},
                new byte?[] { 0xFF, 0xD8, 0xFF, 0xE1, null, null, 0x45, 0x78, 0x69, 0x66, 0x00 },
                new byte?[] { 0xFF, 0xD8, 0xFF, 0xEE }),
            new FormatMagicData(MediaType.Image, FileBinaryFormat.Png, ".png", 
                new byte?[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            new FormatMagicData(MediaType.Image, FileBinaryFormat.Heic, ".heic", 
                new byte?[] { null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63, 0x00 }),
            new FormatMagicData(MediaType.Video, FileBinaryFormat.QuickTime, ".mov",
                new byte?[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x71, 0x74, 0x20, 0x20 }),
            new FormatMagicData(MediaType.Image, FileBinaryFormat.CanonRawCr2, ".cr2", 
                new byte?[] { 0x49, 0x49, 0x2A, 0x00, 0x10, 0x00, 0x00, 0x00, 0x43, 0x52, 0x02, 0x00 })
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


        /** Evaluates the bindary file format for a file by observing the headers in the file **/
        public FormatMagicData DetermineFileFormat(string pathToBinaryFile)
        {
            using (var file = File.OpenRead(pathToBinaryFile))
            {
                byte[] fileHeader = new byte[MaxBytesToRead];
                int bytesRead = file.Read(fileHeader, 0, fileHeader.Length);

                var matchingFormats = from f in Formats
                                      where f.HeaderMatches(fileHeader, bytesRead)
                                      select f;

                if (matchingFormats.Any())
                {
                    return matchingFormats.First();
                }
            }

            return new FormatMagicData(MediaType.Unknown, FileBinaryFormat.Unknown, Path.GetExtension(pathToBinaryFile));
        }

        

        public string ReadFileHeader(FileInfo file)
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
                outputString.Append(Convert.ToChar(headers[index]));
                outputString.Append(" ");
            }

            return $"Hex: {outputHex}\nChr: {outputString}";
        }

        /// <summary>
        /// Return the expected file extension for a file format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public string FileFormatExpectedExtension(FileBinaryFormat format)
        {
            var extension = from f in Formats
                            where f.Format == format
                            select f.Extension;
            return extension.FirstOrDefault();
        }
    }

    public enum FileBinaryFormat
    {
        Unknown = 0,
        Jpeg,
        Png,
        CanonRawCr2,
        Gif,
        Avi,
        Mov,
        Mp4,
        Heic,
        QuickTime
    }

}

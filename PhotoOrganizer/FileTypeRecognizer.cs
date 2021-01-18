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

        public enum FileFormatDescription 
        {
            Unknown = 0,
            Jpeg,
            Png,
            CanonCr2,
            Gif,
            Avi,
            Mov,
            Mp4
        }


        private static readonly byte[] JPEG_HEADER = new byte[] { 0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46, 0x00 };
        private static readonly byte[] PNG_HEADER = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] CR2_HEADER = new byte[] { };
        private static readonly byte[] GIF_HEADER = new byte[] { };

        private static readonly Dictionary<FileFormatDescription, byte[]> FileSignatures;

        static FileTypeRecognizer()
        {
            FileSignatures = new Dictionary<FileFormatDescription, byte[]>
            {
                {FileFormatDescription.Jpeg, JPEG_HEADER },
                {FileFormatDescription.Png, PNG_HEADER },
                {FileFormatDescription.CanonCr2, CR2_HEADER },
                {FileFormatDescription.Gif, GIF_HEADER }
            };
        }



        /** Evaluates the bindary file format for a file by observing the headers in the file **/
        public FileFormatDescription DetermineFileFormat(string pathToBinaryFile)
        {

            int bytesToRead = (from headers in FileSignatures.Values
                               select headers.Length).Max();

            var file = File.OpenRead(pathToBinaryFile);
            byte[] fileHeader = new byte[bytesToRead];
            int bytesRead = file.Read(fileHeader, 0, bytesToRead);

            var matchingFormats = from sig in FileSignatures
                                  where ByteStreamsAreEqual(fileHeader, sig.Value, bytesRead)
                                  select sig.Key;

            if (matchingFormats.Any())
            {
                return matchingFormats.First();
            }

            return FileFormatDescription.Unknown;
        }


        private bool ByteStreamsAreEqual(byte[] source, byte[] example, int bytesRead)
        {
            int comparisonLength = new List<int> { source.Length, example.Length, bytesRead }.Max();
            if (comparisonLength == 0)
                return false;

            for(int index=0; index<comparisonLength; index++)
            {
                if (source[index] != example[index])
                {
                    return false;
                }
            }

            return true;
        }


    }
}

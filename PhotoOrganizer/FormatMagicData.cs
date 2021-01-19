using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class FormatMagicData
    {
        public IEnumerable<byte?[]> Headers { get; private set; }
        //public byte?[] Header { get; set; }
        public FileBinaryFormat Format { get; set; }
        public string Extension { get; set; }
        public MediaType Type { get; set; }

        public FormatMagicData() { }
        public FormatMagicData(MediaType type, FileBinaryFormat format, string extension, byte?[] header)
        {
            Headers = new List<byte?[]> { header };
            Format = format;
            Extension = extension;
            Type = type;
        }

        public FormatMagicData(MediaType type, FileBinaryFormat format, string extension, params byte?[][] headers)
        {
            Headers = new List<byte?[]>(headers);
            Format = format;
            Extension = extension;
            Type = type;
        }


        public bool HeaderMatches(byte[] actualHeader, int bytesRead)
        {
            var matchingFormats = from f in Headers
                                  where ByteStreamsAreEqual(actualHeader, f, bytesRead)
                                  select f;
            return matchingFormats.Any();
        }

        private bool ByteStreamsAreEqual(byte[] source, byte?[] example, int bytesRead)
        {
            if (bytesRead > source.Length)
            {
                throw new ArgumentException("bytesRead cannot be longer than the source array");
            }

            int comparisonLength = new List<int> { source.Length, example.Length, bytesRead }.Min();
            if (comparisonLength == 0)
            {
                return false;
            }

            for (int index = 0; index < comparisonLength; index++)
            {
                if (example[index].HasValue && source[index] != example[index].Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class FormatMagicData
    {
        public IEnumerable<IRangeMatch> Headers { get; private set; }
        public FileBinaryFormat Format { get; set; }
        public string Extension { get; set; }
        public MediaType Type { get; set; }

        public FormatMagicData() { }
        public FormatMagicData(MediaType type, FileBinaryFormat format, string extension, IRangeMatch header)
        {
            Headers = new List<IRangeMatch> { header };
            Format = format;
            Extension = extension;
            Type = type;
        }

        public FormatMagicData(MediaType type, FileBinaryFormat format, string extension, params IRangeMatch[] headers)
        {
            Headers = new List<IRangeMatch>(headers);
            Format = format;
            Extension = extension;
            Type = type;
        }


        public bool HeaderMatches(byte[] actualHeader, int bytesRead)
        {
            var matchingFormats = from f in Headers
                                  where ByteStreamsAreEqual(actualHeader, f.GetRange(), bytesRead)
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

    public interface IRangeMatch
    {
        byte?[] GetRange();
        int Length { get; }
    }

    public class TextRangeMatch : IRangeMatch
    {
        public string Text { get; set; }
        public int Offset { get; set; }

        public TextRangeMatch()
        {

        }

        public TextRangeMatch(int offset, string text)
        {
            Offset = offset;
            Text = text;
        }

        public byte?[] GetRange()
        {
            var bytes = new List<byte?>();
            for (int i = 0; i < Offset; i++)
            {
                bytes.Add(null);
            }

            var textBytes = Encoding.UTF8.GetBytes(Text);
            bytes.AddRange(from b in textBytes select (byte?)b);

            return bytes.ToArray();
        }

        public int Length
        {
            get { return Offset + Text.Length; }
        }
    }

    public class ByteRangeMatch : IRangeMatch
    {
        public byte?[] ByteRange { get; set; }

        public ByteRangeMatch() { }

        public ByteRangeMatch(params byte?[] bytes)
        {
            this.ByteRange = bytes;
        }

        public byte?[] GetRange()
        {
            return this.ByteRange;
        }

        public int Length
        {
            get
            {
                return ByteRange.Length;
            }
        }
    }
}

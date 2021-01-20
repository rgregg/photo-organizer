using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public class FormatSignature
    {
        public IEnumerable<IRangeMatch> Headers { get; private set; }
        public BinaryFormat Format { get; set; }
        public IEnumerable<string> Extensions { get; set; }
        public MediaType Type { get; set; }
        public bool Excluded { get; set; }

        public FormatSignature() { }
        public FormatSignature(MediaType type, BinaryFormat format, string extension, IRangeMatch header)
        {
            Headers = new List<IRangeMatch> { header };
            Format = format;
            Extensions = new List<string> { extension };
            Type = type;
        }

        public FormatSignature(MediaType type, BinaryFormat format, string extension, params IRangeMatch[] headers)
        {
            Headers = new List<IRangeMatch>(headers);
            Format = format;
            Extensions = new List<string> { extension };
            Type = type;
        }

        public FormatSignature(MediaType type, BinaryFormat format, string[] extensions, params IRangeMatch[] headers)
        {
            Headers = new List<IRangeMatch>(headers);
            Format = format;
            Type = type;
            Extensions = new List<string>(extensions);
        }

        /// <summary>
        /// Check if this instance matches a given file extension (.mpg for example)
        /// </summary>
        /// <param name="actualExtension"></param>
        /// <returns></returns>
        public bool ExtensionMatches(string actualExtension)
        {
            return (from e in Extensions
                    where e.Equals(actualExtension, StringComparison.OrdinalIgnoreCase)
                    select e).Any();
        }

        /// <summary>
        /// Check if this instance matches a given file header from the binary file
        /// </summary>
        /// <param name="actualHeader"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        public bool HeaderMatches(byte[] actualHeader, int bytesRead)
        {
            var matchingFormats = from f in Headers
                                  where ByteStreamsAreEqual(actualHeader, f.GetRange(), bytesRead)
                                  select f;
            return matchingFormats.Any();
        }

        /// <summary>
        /// Compare two byte arrays to see if they are equivelent.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="example"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
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
                // We use null to indicate a Wildcard, so if !HasValue we ignore the comparison
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
        public static readonly byte? Wildcard = null;

        public byte?[] ByteRange { get; set; }

        public ByteRangeMatch() { }

        public ByteRangeMatch(params byte?[] bytes)
        {
            this.ByteRange = bytes;
        }

        public ByteRangeMatch(string byteFormat)
        {
            // input string is formatted as follows:
            // 01 02 03 04 AB CD ?? 00
            // where bytes are separated by spaces. ?? means a wildcard.

            var components = byteFormat.Split(' ');
            List<byte?> range = new List<byte?>();
            for(int i = 0; i < components.Length; i++)
            {
                if (components[i].Equals("??"))
                {
                    range.Add(Wildcard);
                }
                else
                {
                    range.Add(Convert.ToByte(components[i], 16));
                }
            }
            ByteRange = range.ToArray();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public static class FileExtensionMethods
    {
        /// <summary>
        /// Compare the name, size, and SHA1 hash of two files to see if they are identical 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static bool IsFileIdentical(this IFile source, IFile destination, FileComparison comparisons = FileComparison.All)
        {
            if (comparisons.IsFlagSet(FileComparison.Filename) &&
                source.Name != destination.Name)
            {
                return false;
            }

            if (comparisons.IsFlagSet(FileComparison.Length) &&
                source.Length != destination.Length)
            {
                return false;
            }

            if (comparisons.IsFlagSet(FileComparison.DateTimeModified) &&
                source.DateTimeLastModified != destination.DateTimeLastModified)
            {
                return false;
            }

            if (comparisons.IsFlagSet(FileComparison.SHA1Hash))
            {
                var source_crc = source.ComputeSha1Hash();
                var dest_crc = destination.ComputeSha1Hash();
                if (source_crc != dest_crc)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

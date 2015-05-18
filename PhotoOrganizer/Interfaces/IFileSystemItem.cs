using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public interface IFileSystemItem
    {
        /// <summary>
        /// Gets the full path to the file
        /// </summary>
        string FullName { get; }

        string Name { get; }

        bool Exists { get; }

    }

    public enum ExistingFileMode
    {
        Abort = 0,
        Ignore,
        Rename,
        Overwrite,
        DeleteSourceFileWhenIdentical
    }
}

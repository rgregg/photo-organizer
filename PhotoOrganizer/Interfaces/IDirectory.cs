using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public interface IDirectory : IFileSystemItem
    {
        IEnumerable<IFile> EnumerateFiles();
        IEnumerable<IDirectory> EnumerateDirectories();

        /// <summary>
        /// Returns a reference to a particular descendent folder by name
        /// </summary>
        /// <param name="childDirectoryName"></param>
        /// <returns></returns>
        IDirectory GetChildDirectory(string childDirectoryName);

        /// <summary>
        /// Creates the directory if necessary
        /// </summary>
        void Create();

        IFile GetFile(string filename);
    }
}

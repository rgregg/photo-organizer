using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    public interface IDirectory : IFileSystemItem
    {
        Task<IEnumerable<IFile>> EnumerateFilesAsync();
        Task<IEnumerable<IDirectory>> EnumerateDirectoriesAsync();

        /// <summary>
        /// Returns a reference to a particular descendent folder by name
        /// </summary>
        /// <param name="childDirectoryName"></param>
        /// <returns></returns>
        Task<IDirectory> GetChildDirectoryAsync(string childDirectoryName);

        /// <summary>
        /// Creates the directory if necessary
        /// </summary>
        Task CreateAsync();

        Task<IFile> GetFileAsync(string filename);
    }
}

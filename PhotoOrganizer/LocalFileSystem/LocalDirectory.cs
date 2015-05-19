using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer.LocalFileSystem
{
    class LocalDirectory : IDirectory
    {
        private readonly DirectoryInfo _directory;

        public LocalDirectory(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public LocalDirectory(string path)
        {
            _directory = new DirectoryInfo(path);
        }

        public async Task<IEnumerable<IFile>> EnumerateFilesAsync()
        {
            var items = from p in _directory.EnumerateFiles()
                        select new LocalFile(p);
            return items;
        }

        public async Task<IEnumerable<IDirectory>> EnumerateDirectoriesAsync()
        {
            var items = from d in _directory.EnumerateDirectories()
                        select new LocalDirectory(d);
            return items;
        }

        public string FullName
        {
            get { return _directory.FullName; }
        }

        public string Name
        {
            get { return _directory.Name; }
        }

        public async Task<IDirectory> GetChildDirectoryAsync(string childDirectoryName)
        {
            var childPath = Path.Combine(_directory.FullName, childDirectoryName);
            return new LocalDirectory(new DirectoryInfo(childPath));
        }

        public async Task CreateAsync()
        {
            _directory.Create();
        }

        public async Task<IFile> GetFileAsync(string filename)
        {
            var filePath = Path.Combine(_directory.FullName, filename);
            return new LocalFile(new FileInfo(filePath));
        }

        public bool Exists { get { return _directory.Exists; } }
    }
}

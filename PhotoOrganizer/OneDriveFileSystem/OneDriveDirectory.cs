using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer.OneDriveFileSystem
{
    class OneDriveDirectory : IDirectory
    {
        public static OneDrive.ODConnection Connection {get; set;}

        public IEnumerable<IFile> EnumerateFiles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IDirectory> EnumerateDirectories()
        {
            throw new NotImplementedException();
        }

        public IDirectory GetChildDirectory(string childDirectoryName)
        {
            throw new NotImplementedException();
        }

        public void Create()
        {
            throw new NotImplementedException();
        }

        public IFile GetFile(string filename)
        {
            throw new NotImplementedException();
        }

        public string FullName
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public bool Exists
        {
            get { throw new NotImplementedException(); }
        }
    }
}

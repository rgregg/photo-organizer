using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer.OneDriveFileSystem
{
    class OneDriveFile : IFile
    {
        public long Length
        {
            get { throw new NotImplementedException(); }
        }

        public void CopyTo(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFileName = null)
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset DateTimeLastModified
        {
            get { throw new NotImplementedException(); }
        }

        public string ComputeSha1Hash()
        {
            throw new NotImplementedException();
        }

        public DetailFileInfo.PerceivedFileType PerceivedType
        {
            get { throw new NotImplementedException(); }
        }

        public DateTimeOffset? DateTaken
        {
            get { throw new NotImplementedException(); }
        }

        public string CameraMake
        {
            get { throw new NotImplementedException(); }
        }

        public string CameraModel
        {
            get { throw new NotImplementedException(); }
        }

        public IDirectory CurrentDirectory
        {
            get { throw new NotImplementedException(); }
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

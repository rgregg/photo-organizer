using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer 
{
    public interface IFile : IFileSystemItem
    {
        long Length { get; }

        void CopyTo(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort);
        void MoveTo(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFileName = null);
        void Delete();


        DateTimeOffset DateTimeLastModified { get; }

        string ComputeSha1Hash();

        DetailFileInfo.PerceivedFileType PerceivedType { get; }
        DateTimeOffset? DateTaken { get; }
        string CameraMake { get; }
        string CameraModel { get; }

        IDirectory CurrentDirectory { get; }
    }
}

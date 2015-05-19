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

        Task CopyToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort);
        Task MoveToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFileName = null);
        Task DeleteAsync();


        DateTimeOffset DateTimeLastModified { get; }

        string ComputeSha1Hash();

        DetailFileInfo.PerceivedFileType PerceivedType { get; }
        DateTimeOffset? DateTaken { get; }
        string CameraMake { get; }
        string CameraModel { get; }

        IDirectory CurrentDirectory { get; }
    }
}

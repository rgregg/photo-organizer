using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDrive;

namespace PhotoOrganizer.OneDriveFileSystem
{
    class OneDriveFile : IFile
    {
        protected ODItem _item;
        private ODConnection _connection;

        public OneDriveFile(ODConnection connection, ODItem item)
        {
            _connection = connection;
            _item = item;
        }


        public long Length
        {
            get { return _item.Size; }
        }

        public async Task CopyToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort)
        {
            throw new NotImplementedException();
        }

        public static async Task<ODItemReference> CollapseItemReferenceAsync(ODItemReference itemRef)
        {
            if (itemRef is ODExtendedItemReference)
            {
                var newItemRef = new ODItemReference();
                newItemRef.DriveId = itemRef.DriveId;

                var additionalPath = ((ODExtendedItemReference)itemRef).AdditionalPath;

                if (!string.IsNullOrEmpty(additionalPath))
                {
                    if (itemRef.Path != null)
                    {
                        if (!additionalPath.StartsWith("/")) 
                            additionalPath = "/" + additionalPath;
                        newItemRef.Path = itemRef.Path + additionalPath;
                    }
                    else
                    {
                        // Could make a call to GetItemAsync to resolve this case
                        throw new ArgumentException("Cannot collpase item reference when base path is not provided.");
                    }
                }
                else
                {
                    newItemRef.Path = itemRef.Path;
                    newItemRef.Id = itemRef.Id;
                }
                return newItemRef;
            }
            return itemRef;
        }

        public async Task MoveToAsync(IDirectory targetDirectory, ExistingFileMode fileExistsBehavior = ExistingFileMode.Abort, string newFileName = null)
        {
            OneDriveDirectory targetDir = targetDirectory as OneDriveDirectory;
            if (null == targetDir) throw new ArgumentException("targetDirectory must be a OneDriveDirectory object");

            var updatedItemProps = new ODItem()
            {
                ParentReference = await CollapseItemReferenceAsync(targetDir.ItemReference)
            };

            //switch (fileExistsBehavior)
            //{
            //    case ExistingFileMode.Abort:
            //    case ExistingFileMode.Ignore:
            //    case ExistingFileMode.DeleteSourceFileWhenIdentical:
            //        updatedItemProps.NameConflictBehaiorAnnotation = NameConflictBehavior.Fail;
            //        break;
            //    case ExistingFileMode.Overwrite:
            //        updatedItemProps.NameConflictBehaiorAnnotation = NameConflictBehavior.Replace;
            //        break;
            //    case ExistingFileMode.Rename:
            //        updatedItemProps.NameConflictBehaiorAnnotation = NameConflictBehavior.Rename;
            //        break;
            //}

            try
            {
                _item = await _connection.PatchItemAsync(_item.ItemReference(), updatedItemProps);
            }
            catch (ODException ex)
            {
                // TODO: handle the abort/ignore/delete cases
                if (ex is ODServerException)
                {
                    var exp = (ODServerException)ex;
                    Console.WriteLine("Error moving {0}: {1} [{2}]", this.Name, exp.ServiceError.Message, exp.ServiceError.Code);
                }
                else
                {
                    Console.WriteLine("Error moving {0}: {1}", this.Name, ex.Message);
                }
                
            }
        }

        public async Task DeleteAsync()
        {
            await _connection.DeleteItemAsync(_item.ItemReference(), ItemDeleteOptions.Default);
        }

        public DateTimeOffset DateTimeLastModified
        {
            get { return _item.LastModifiedDateTime; }
        }

        public string ComputeSha1Hash()
        {
            if (null != _item.File && null != _item.File.Hashes)
            {
                return _item.File.Hashes.Sha1;
            }

            return null;
        }

        public DetailFileInfo.PerceivedFileType PerceivedType
        {
            get 
            {
                if (_item.Photo != null || _item.Image != null)
                    return DetailFileInfo.PerceivedFileType.Image;
                if (_item.Video != null)
                    return DetailFileInfo.PerceivedFileType.Video;
                
                return DetailFileInfo.PerceivedFileType.Unspecified;
            }
        }

        public DateTimeOffset? DateTaken
        {
            get 
            {
                if (_item.Photo != null)
                    return _item.Photo.TakenDateTime;
                return null;
            }
        }

        public string CameraMake
        {
            get 
            { 
                if (_item.Photo != null) 
                {
                    return _item.Photo.CameraMake;
                } 

                return null;
            }
        }

        public string CameraModel
        {
            get
            {
                if (_item.Photo != null)
                {
                    return _item.Photo.CameraModel;
                }

                return null;
            }
        }

        public IDirectory CurrentDirectory
        {
            get 
            {
                throw new NotImplementedException(); 
            }
        }

        public string FullName
        {
            get { return _item.Path(false); }
        }

        public string Name
        {
            get { return _item.Name; }
        }

        public bool Exists
        {
            get { return !string.IsNullOrEmpty(_item.Id); }
        }
    }
}

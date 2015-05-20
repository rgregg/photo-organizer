using Nito.AsyncEx;
using OneDrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer.OneDriveFileSystem
{
    class OneDriveDirectory : IDirectory
    {
        private OneDrive.ODConnection _connection;
        private OneDrive.ODItemReference _itemReference;

        private List<OneDrive.ODItem> _contents = new List<OneDrive.ODItem>();

        public OneDriveDirectory(OneDrive.ODConnection connection, string path)
        {
            _connection = connection;
            _itemReference = OneDrive.ODConnection.ItemReferenceForDrivePath(path);

            FullName = path;
            Name = System.IO.Path.GetDirectoryName(path);
        }

        public OneDriveDirectory(ODConnection connection, ODItem item)
        {
            _connection = connection;
            _itemReference = item.ItemReference();

            FullName = item.Path(false);
            Name = item.Name;
        }

        public OneDriveDirectory(ODConnection connection, ODItemReference itemRef)
        {
            _connection = connection;
            _itemReference = itemRef;

            Task<ODItemReference> task = OneDriveFile.CollapseItemReferenceAsync(itemRef);
            task.Wait();
            var newItemRef = task.Result;
            FullName = newItemRef.Path;
        }

        public ODItemReference ItemReference { get { return _itemReference; } }

        private async Task DownloadFolderContents()
        {
            if (_contents.Any())
                return;

            Console.WriteLine("Downloading folder contents from OneDrive...");
            var options = new ChildrenRetrievalOptions { SelectProperties = "id,name,size,lastModifiedDateTime,file,folder,photo,video,image".Split(','), PageSize = 500 };

            ODItemCollection children = await _connection.GetChildrenOfItemAsync(_itemReference, options);
            _contents.AddRange(children.Collection);

            while (children.MoreItemsAvailable())
            {
                Console.WriteLine("{0} items found.", _contents.Count);
                children = await children.GetNextPage(_connection);
                _contents.AddRange(children.Collection);
            }
            Console.WriteLine("{0} items found.", _contents.Count);
        }

        public async Task<IEnumerable<IFile>> EnumerateFilesAsync()
        {
            await DownloadFolderContents();
            return (from item in _contents where item.File != null select new OneDriveFile(_connection, item));
        }

        public async Task<IEnumerable<IDirectory>> EnumerateDirectoriesAsync()
        {
            await DownloadFolderContents();
            return (from item in _contents where item.Folder != null select new OneDriveDirectory(_connection, item));
        }

        private Dictionary<string, OneDriveDirectory> _directoryCache = new Dictionary<string, OneDriveDirectory>();

        private readonly Nito.AsyncEx.AsyncSemaphore _directoryCacheLock = new AsyncSemaphore(1);

        public async Task<IDirectory> GetChildDirectoryAsync(string childDirectoryName)
        {
            childDirectoryName = childDirectoryName.Replace(@"\", "/");

            OneDriveDirectory childDirectory;
            if (_directoryCache.TryGetValue(childDirectoryName, out childDirectory))
                return childDirectory;
            else
            {
                //await _directoryCacheLock.WaitAsync();
                
                var dirRef = _itemReference.AddPathComponent(childDirectoryName);
                childDirectory = new OneDriveDirectory(_connection, _itemReference.AddPathComponent(childDirectoryName));
                _directoryCache[childDirectoryName] = childDirectory;
                
                //_directoryCacheLock.Release();
                return childDirectory;
            }
        }

        private readonly AsyncSemaphore _createLock = new AsyncSemaphore(1);

        public async Task CreateAsync()
        {
            await _createLock.WaitAsync();

            if (!string.IsNullOrEmpty(_itemReference.Id))
            {
                return;
            }

            try
            {
                var item = await _connection.PatchItemAsync(_itemReference, new ODItem { Folder = new OneDrive.Facets.FolderFacet() });
                _itemReference = item.ItemReference();
            }
            catch (ODException ex)
            {
                Console.WriteLine("Error while creating folder: {0}", ex.Message);
            }
            finally
            {
                _createLock.Release();
            }
        }

        public async Task<IFile> GetFileAsync(string filename)
        {
            var itemRef = _itemReference.AddPathComponent(filename);
            var item = await _connection.GetItemAsync(itemRef, ItemRetrievalOptions.Default);
            return new OneDriveFile(_connection, item);
        }

        public string FullName
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool Exists
        {
            get { return true; }
        }
    }
}

using CommandLine.Text;
using OneDrive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicatePhotoFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            var opts = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                Console.WriteLine(HelpText.AutoBuild(opts));
                return;
            }

            var auth = new SimpleAuth { AccessToken = opts.AccessToken };

            Nito.AsyncEx.AsyncContext.Run(() => EnumerateItemsFromOneDrive(auth));
        }


        private class SimpleAuth : OneDrive.IAuthenticationInfo
        {
            public string AccessToken { get;set;}

            public string AuthorizationHeaderValue
            {
                get { return string.Format("Bearer {0}", AccessToken); }
            }

            public Task<bool> RefreshAccessTokenAsync()
            {
                return Task.FromResult(true);
            }

            public string RefreshToken { get;set;}

            public DateTimeOffset TokenExpiration
            {
                get { return DateTimeOffset.MaxValue; }
            }

            public string TokenType
            {
                get { return "Bearer"; }
            }
        }

        private static async Task EnumerateItemsFromOneDrive(IAuthenticationInfo auth)
        {
            ODConnection connection = new OneDrive.ODConnection("https://api.onedrive.com/v1.0", auth);

            ODItemReference photosFolder = ODConnection.ItemReferenceForSpecialFolder("photos");

            Console.Write("Loading...");

            bool hasMoreChanges = true;
            string nextToken = null;
            while (hasMoreChanges)
            {
                var nextPageOfChanges = await connection.ViewChangesAsync(photosFolder, new ViewChangesOptions 
                {
                    StartingToken = nextToken,
                    PageSize = 1000,
                    Select = "id,name,size,lastModifiedDateTime,file,folder,photo,parentReference"
                });

                ProcessFoundItems(connection, nextPageOfChanges.Collection);

                hasMoreChanges = nextPageOfChanges.HasMoreChanges;
                nextToken = nextPageOfChanges.NextToken;

                Console.Write(".");
            }

            // Enumerate identical items
            var identicalFiles = from f in itemHashTable
                                 where f.Value.Count > 1
                                 select f;

            Console.WriteLine();

            foreach (var item in identicalFiles)
            {
                Console.WriteLine("SHA1: {0}", item.Key);
                var matchingFiles = item.Value;
                foreach (var file in matchingFiles)
                {
                    Console.WriteLine("  {0}: {1}", file.Name, PathForItem(file));
                }
            }
            Console.ReadKey();
        }

        private static string PathForItem(ODItem item)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(item.Name);
            sb.Insert(0, "/");
            var parentItemRef = item.ParentReference;
            while (null != parentItemRef)
            {
                ODItem parentItem;
                if (folderLookupTable.TryGetValue(parentItemRef.Id, out parentItem))
                {
                    sb.Insert(0, parentItem.Name);
                    sb.Insert(0, "/");
                    parentItemRef = parentItem.ParentReference;
                }
                else
                {
                    parentItemRef = null;
                }
            }
            return sb.ToString();
        }

        private static void ProcessFoundItems(ODConnection connection, ODItem[] items)
        {
            foreach (var item in items)
            {

                if (item.Folder != null)
                {
                    folderLookupTable[item.Id] = item;
                }

                // Ignore things that aren't files
                if (item.File == null)
                {
                    continue;
                }
                
                // Check to see if the item has a sha1 hash on it
                string sha1Hash = null;
                if (item.File.Hashes != null && !string.IsNullOrEmpty(item.File.Hashes.Sha1))
                {
                    sha1Hash = item.File.Hashes.Sha1;
                }
                else
                {
                    itemWithNullHash.Add(item);
                    continue;
                }

                // Add the item to the hash table
                List<ODItem> matchingItems;
                if (itemHashTable.TryGetValue(sha1Hash, out matchingItems))
                {
                    matchingItems.Add(item);
                }
                else
                {
                    matchingItems = new List<ODItem>();
                    matchingItems.Add(item);
                    itemHashTable[sha1Hash] = matchingItems;
                }
            }
        }

        private static Dictionary<string, List<ODItem>> itemHashTable = new Dictionary<string, List<ODItem>>();
        private static List<ODItem> itemWithNullHash = new List<ODItem>();
        private static Dictionary<string, ODItem> folderLookupTable = new Dictionary<string, ODItem>();
    }
}

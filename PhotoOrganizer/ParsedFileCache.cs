using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace PhotoOrganizer
{
    public class ParsedFileCache
    {
        private const int MAX_CACHE_SIZE = 100000;
        private string CACHE_PATH;

        private Dictionary<String, MediaInfo> memoryCache;

        public ParsedFileCache(string basePath)
        {
            CACHE_PATH = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "PhotoOrganizerCache");
            Directory.CreateDirectory(CACHE_PATH);

            CACHE_PATH = Path.Combine(CACHE_PATH, ComputeSha256Hash(basePath) + ".json");
            LoadCache();
        }

        public void Add(MediaInfo parsedFile)
        {
            memoryCache[ComputeSha256Hash(parsedFile.FullPath)] = parsedFile;
            HasChanged = true;
        }

        public void Remove(string path)
        {
            string key = ComputeSha256Hash(path);
            memoryCache.Remove(key);
            HasChanged = true;
        }

        public bool CacheLookup(FileInfo file, out MediaInfo storedEntity)
        {
            MediaInfo cacheHit = null;
            if (memoryCache.TryGetValue(ComputeSha256Hash(file.FullName), out cacheHit))
            {
                if (cacheHit.Size == file.Length && 
                    cacheHit.Created.Equals(new DateTimeOffset(file.CreationTimeUtc)) && 
                    cacheHit.LastModified.Equals(new DateTimeOffset(file.LastWriteTimeUtc)))
                {
                    storedEntity = cacheHit;
                    return true;
                }
            }

            storedEntity = null;
            return false;
        }

        public bool HasChanged
        {
            get; set;
        }

        public void PersistCache()
        {
            string cache = JsonConvert.SerializeObject(memoryCache);
            File.WriteAllText(CACHE_PATH, cache);
        }

        private void LoadCache()
        {
            string cached = "";
            try 
            {
                cached = File.ReadAllText(CACHE_PATH);
                memoryCache = JsonConvert.DeserializeObject<Dictionary<String, MediaInfo>>(cached);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading folder cache. We'll start a fresh one. {ex.Message}");
                memoryCache = new Dictionary<string, MediaInfo>();
            }
        }

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    }
}

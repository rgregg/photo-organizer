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
    public class ParserCache
    {
        private const int MAX_CACHE_SIZE = 100000;
        private string CACHE_PATH;
        private readonly ILogWriter LogWriter;

        private Dictionary<string, CacheEntry> memoryCache;

        public ParserCache(string cachePath, ILogWriter logWriter)
        {
            LogWriter = logWriter;

            CACHE_PATH = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "PhotoOrganizerCache");
            Directory.CreateDirectory(CACHE_PATH);

            CACHE_PATH = Path.Combine(CACHE_PATH, ComputeSha256Hash(cachePath) + ".json");
            LoadCache();
        }

        public void ClearAll()
        {
            memoryCache.Clear();
        }

        public void Add(FileInfo file, MediaMetadata metadata, FormatSignature signature)
        {
            CacheEntry entry = new CacheEntry()
            {
                Format = signature.Format,
                Metadata = metadata,
                Type = signature.Type,
                Size = file.Length,
                LastModified = file.LastWriteTimeUtc
            };

            var key = ComputeSha256Hash(file.FullName);
            memoryCache[key] = entry;
            CacheHasChanged = true;
        }

        public void Remove(string path)
        {
            string key = ComputeSha256Hash(path);
            memoryCache.Remove(key);
            CacheHasChanged = true;
        }

        public bool TryCacheLookup(FileInfo file, out CacheEntry result)
        {
            var key = ComputeSha256Hash(file.FullName);

            if (memoryCache.TryGetValue(key, out CacheEntry storedData))
            {
                LogWriter.WriteLog($"Cache: Found a record for {file.FullName}", true);
                if (storedData.Size == file.Length &&
                    storedData.LastModified.Equals(new DateTimeOffset(file.LastWriteTimeUtc)))
                {
                    LogWriter.WriteLog($"Cache: Hit for {file.FullName}", true);
                    result = storedData;
                    return true;
                }
                else
                {
                    LogWriter.WriteLog($"Cache: Miss for {file.FullName}, {storedData.Size}->{file.Length}; {storedData.LastModified}->{file.LastWriteTimeUtc}", true);
                }
            }

            LogWriter.WriteLog($"Cache: Miss for {file.FullName}", true);
            result = null;
            return false;
        }

        public bool CacheHasChanged { get; set; }

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
                memoryCache = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(cached);
            }
            catch (Exception ex)
            {
                if (null != LogWriter)
                {
                    LogWriter.WriteLog($"Error loading folder cache. We'll start a fresh one. {ex.Message}", false);
                }
                memoryCache = new Dictionary<string, CacheEntry>();
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

    public class CacheEntry
    {
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public MediaMetadata Metadata { get; set; }
        public MediaType Type { get; set; }
        public BinaryFormat Format { get; set; }
    }
}

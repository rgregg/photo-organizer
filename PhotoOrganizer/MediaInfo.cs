using Newtonsoft.Json;
using System;


namespace PhotoOrganizer
{

    public class MediaInfo
    {
        [JsonIgnore]
        public virtual string Filename { get; set; }
        [JsonIgnore]
        public virtual string FullPath { get; set; }
        public virtual DateTimeOffset Created { get; set; }
        public virtual DateTimeOffset LastModified { get; set; }
        public virtual long Size { get; set; }
        public virtual DateTimeOffset? Taken { get; set; }
        public virtual string CameraMake { get; set; }
        public virtual string CameraModel { get; set; }
        public virtual MediaType Type { get; set; }
    }

    public enum MediaType
    {
        Unknown,
        Image,
        Video,
        Document,
        Audio
    }

}


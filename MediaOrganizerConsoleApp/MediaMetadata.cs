using Newtonsoft.Json;
using System;


namespace MediaOrganizerConsoleApp
{
    public class MediaMetadata
    {
        // Media properties
        public virtual DateTimeOffset? DateCaptured { get; set; }
        public virtual string CameraMake { get; set; }
        public virtual string CameraModel { get; set; }
    }

    public enum MediaType
    {
        Unknown,
        Image,
        Video,
        Metadata,
        System
    }

}


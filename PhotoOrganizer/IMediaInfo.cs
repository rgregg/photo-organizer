using System;

namespace PhotoOrganizer
{
    public interface IMediaInfo
    {
        string Filename {get;}
        string Path {get;}

        DateTimeOffset Created {get;}
        DateTimeOffset LastModified {get;}

        DateTimeOffset? Taken {get;}

        string CameraMake {get;}
        string CameraModel {get;}

        MediaType Type {get;}
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


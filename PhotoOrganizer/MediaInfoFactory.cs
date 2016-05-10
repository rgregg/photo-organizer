using System;

namespace PhotoOrganizer
{
    public static class MediaInfoFactory
    {
        public static IMediaInfo GetMediaInfo(System.IO.FileInfo file) 
        {
            return new TagLibFileInfo(file);
        }
    }
}


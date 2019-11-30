using System;

namespace PhotoOrganizer
{
    public static class MediaInfoFactory
    {
        public static IMediaInfo GetMediaInfo(System.IO.FileInfo file, DataParser parser) 
        {
            switch(parser)
            {
                case DataParser.Shell32:
                    return new ShellExtendedFileInfo(file);
                case DataParser.TagLib:
                case DataParser.Default:
                    return new TagLibFileInfo(file);
                case DataParser.Universal:
                    return TryMultipleSources(file);
                default:
                    throw new Exception("Unsupported data parser.");
            }
        }

        private static IMediaInfo TryMultipleSources(System.IO.FileInfo file)
        {
            IMediaInfo parser = new TagLibFileInfo(file);
            if (parser.Type != MediaType.Unknown)
            {
                return parser;
            }

            parser = new ShellExtendedFileInfo(file);
            return parser;
        }
    }
}


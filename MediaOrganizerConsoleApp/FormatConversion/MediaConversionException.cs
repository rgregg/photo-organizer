using System;

namespace MediaOrganizerConsoleApp.FormatConversion
{
    public class MediaConversionException : Exception
    {

        public MediaConversionException(string message) : base(message)
        {

        }

        public MediaConversionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public string StandardOutput { get; internal set; }
        public string StandardError { get; internal set; }
        public int ExitCode { get; internal set; }
    }
}

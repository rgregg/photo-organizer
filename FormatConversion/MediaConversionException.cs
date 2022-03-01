using System;

namespace MediaOrganizerConsoleApp.FormatConversion
{
    public class MediaConversionException : AppInvokerException
    {

        public MediaConversionException(string message) : base(message)
        {

        }

        public MediaConversionException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}

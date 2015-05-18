using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizer
{
    internal static class EnumExtensions
    {

        public static bool IsFlagSet<T>(this T value, T flags) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
            if (!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));

            long longValue = (long)(object)value;
            long longFlags = (long)(object)flags;

            return (longValue & longFlags) == longFlags;
        }

    }
}

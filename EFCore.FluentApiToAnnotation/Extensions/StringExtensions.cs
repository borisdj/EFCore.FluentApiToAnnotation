using System;

namespace EFCore.FluentApiToAnnotation.Extensions
{
    public static class StringExtensions
    {
        public static string[] Split(this string value, string separator, int? count = null)
        {
            if (count != null)
            {
                return value.Split(new string[] { separator }, (int)count, StringSplitOptions.None);
            }
            else
            {
                return value.Split(new string[] { separator }, StringSplitOptions.None);
            }
        }

        public static string Remove(this string value, string substring)
        {
            return value.Remove(new string[] { substring });
        }

        public static string Remove(this string value, string[] substrings)
        {
            foreach (var substring in substrings)
            {
                value = value.Replace(substring, "");
            }
            return value;
        }
    }
}

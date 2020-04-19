using System;
namespace CourseLibrary.API.Helpers
{
    public static class StringExtensions
    {
        public static bool DefaultEquals(this string @string, string value) => @string.Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }
}

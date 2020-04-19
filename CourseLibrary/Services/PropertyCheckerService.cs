using System;
using System.Linq;
using System.Reflection;

namespace CourseLibrary.API.Services
{
    public class PropertyCheckerService : IPropertyCheckerService
    {
        public bool TypeHasProperties<T>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            try
            {
                foreach (var propertyName in fields.Split(',').Select(x => x.Trim()))
                {
                    var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (ArgumentNullException ex)
            {
                return false;
            }
        }
    }
}

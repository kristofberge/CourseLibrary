using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace CourseLibrary.API.Helpers
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields = null) where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!source.Any())
            {
                return new List<ExpandoObject>();
            }

            IEnumerable<PropertyInfo> propertyInfoList =
                string.IsNullOrWhiteSpace(fields) ?
                    GetAllProperties<TSource>() :
                    GetRequestedProperties<TSource>(fields);

            return GetShapedObjects(source, propertyInfoList);
        }

        public static ExpandoObject ShapeData<TSource>(this TSource source, string fields = null) where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IEnumerable<PropertyInfo> propertyInfoList =
                string.IsNullOrWhiteSpace(fields) ?
                    GetAllProperties<TSource>() :
                    GetRequestedProperties<TSource>(fields);

            return GetShapedObject(source, propertyInfoList);
        }

        private static PropertyInfo[] GetAllProperties<TSource>() where TSource : class
            => typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        private static IEnumerable<PropertyInfo> GetRequestedProperties<TSource>(string fields) where TSource : class
            => fields.Split(',').Select(field =>
               {
                   var propertyName = field.Trim();
                   PropertyInfo propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                   return propertyInfo ?? throw new ArgumentNullException($"Property {propertyName} was not found on type {typeof(TSource)}");
               });

        private static IEnumerable<ExpandoObject> GetShapedObjects<TSource>(IEnumerable<TSource> source, IEnumerable<PropertyInfo> propertyInfoList) where TSource : class
            => source.Select(sourceItem => GetShapedObject(sourceItem, propertyInfoList));

        private static ExpandoObject GetShapedObject<TSource>(TSource sourceItem, IEnumerable<PropertyInfo> propertyInfoList) where TSource : class
        {
            var dataShapedObject = new ExpandoObject();

            foreach (var propertyInfo in propertyInfoList)
            {
                var propertyValue = propertyInfo.GetValue(sourceItem);
                ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }
    }
}

using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CourseLibrary.API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {
        private Dictionary<string, PropertyMappingValue> _authorPropertyMapping = new Dictionary<string, PropertyMappingValue>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Id", new PropertyMappingValue(new List<string> { "Id" } ) },
            { "MainCategory", new PropertyMappingValue(new List<string> { "MainCategory" } ) },
            { "Age", new PropertyMappingValue(new List<string> { "DateOfBirth" }, true ) },
            { "Name", new PropertyMappingValue(new List<string> { "FirstName", "LastName" } ) }
        };

        private IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

        public PropertyMappingService()
        {
            _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }

        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            try
            {
                return matchingMapping.Single().MappingDictionary;
            }
            catch
            {
                throw new Exception($"Cannot find property mapping for <{typeof(TSource)}, {typeof(TDestination)}>");
            }
        }

        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            foreach (var field in fields.Split())
            {
                var trimmedField = field.Trim();
                int indexOfFirstSpace = trimmedField.IndexOf(' ');
                var propertyName = indexOfFirstSpace == -1 ?
                    trimmedField : trimmedField.Remove(indexOfFirstSpace);

                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

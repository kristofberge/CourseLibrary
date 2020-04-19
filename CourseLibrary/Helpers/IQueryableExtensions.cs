using CourseLibrary.API.Services;
using System.Linq.Dynamic.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CourseLibrary.API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy, IDictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(mappingDictionary));
            }

            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return source;
            }

            var stringBuilder = new StringBuilder();

            var orderBySplit = orderBy.Split(',');
            foreach (var orderByClause in orderBySplit)
            {
                var orderByClauseTrimmed = orderByClause.Trim();
                bool descending = orderByClauseTrimmed.EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase);
                var indexOfSpace = orderByClauseTrimmed.IndexOf(' ');
                var propertyName = indexOfSpace == -1 ?
                    orderByClauseTrimmed :
                    orderByClauseTrimmed.Remove(indexOfSpace);

                if (mappingDictionary.TryGetValue(propertyName, out PropertyMappingValue propertyMappingValue))
                {

                    foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
                    {
                        if (propertyMappingValue.Revert)
                        {
                            descending = !descending;
                        }

                        _ = stringBuilder
                            .Append(stringBuilder.Length == 0 ? string.Empty : ", ")
                            .Append(destinationProperty)
                            .Append(descending ? " descending" : " ascending");
                    }
                }
            }

            return source.OrderBy(stringBuilder.ToString());
        }
    }
}

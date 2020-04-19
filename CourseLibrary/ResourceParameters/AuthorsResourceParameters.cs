using System;
namespace CourseLibrary.API.ResourceParameters
{
    public class AuthorsResourceParameters
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 10;
        private int _pageNumber = 1;

        public string MainCategory { get; set; }
        public string SearchQuery { get; set; }
        public int PageNumber { get => _pageNumber; set => _pageNumber = Math.Max(1, value); }
        public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, MaxPageSize); }
        public string OrderBy { get; set; } = "Name";
        public string Fields { get; set; }
    }
}

using System.Collections.Generic;

namespace InvoiceManagementApi.Models
{
    public class SimplePagedResult<T>
    {
        public string Message { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public long TotalRecords { get; set; }
        public long TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}

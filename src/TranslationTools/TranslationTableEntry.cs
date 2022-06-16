using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace TranslationTools
{
    class TranslationTableEntry : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string Language { get; set; }
        public string Origin { get; set; }
    }
}

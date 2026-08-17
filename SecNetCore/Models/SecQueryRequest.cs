using System;
using System.Collections.Generic;
using System.Text;

namespace SecNetCore.Models
{
    public class SecQueryRequest
    {
        public SecPageRequest Page { get; set; } = new();

        public string? Search { get; set; }

        public List<SecSort> Sorts { get; set; } = new();
    }
}

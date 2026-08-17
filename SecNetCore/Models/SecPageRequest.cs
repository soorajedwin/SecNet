using System;
using System.Collections.Generic;
using System.Text;

namespace SecNetCore.Models
{
    public class SecPageRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}

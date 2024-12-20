using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Interfaces
{
    public interface IQueryBuilderAdapter
    {
        public string? Convert(object? value);

        public string? Convert(object? value, string format);

        public string Format { get; set; }
    }
}

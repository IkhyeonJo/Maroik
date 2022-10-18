using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.WebSite.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequiredHttpPostAccessAttribute : Attribute
    {
        public string Role { get; set; }
    }
}

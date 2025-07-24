using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos
{
    public class ExternalLoginDto
    {
        public string Provider { get; set; } = string.Empty;
        public string Code { get; set; }
        public string State { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecureStructMvc.Models
{
    public class Details
    {
        public string Name { get; set; }
        public BoltSecureStruct CreditCard { get; set; }
        public string PlainCreditCard { get; set; }
    }
}

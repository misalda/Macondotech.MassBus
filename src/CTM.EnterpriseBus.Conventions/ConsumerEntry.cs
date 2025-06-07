using System;
using System.Collections.Generic;

namespace CTM.EnterpriseBus.Conventions
{
    public class ConsumerEntry
    {
        public Type ContractClassType { get; set; }
        public IEnumerable<Type> ConsumerClassTypes {get;set;}
        public string ReceiveEndPoint { get; set; }
    }
}

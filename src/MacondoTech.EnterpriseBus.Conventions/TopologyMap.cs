using System;
using System.Collections.Generic;

namespace MacondoTech.EnterpriseBus.Conventions
{
    public class TopologyMap 
    {
        public string DefaultEndPoint { get; set; }
        public IEnumerable<ConsumerEntry> EventConsumers { get; set; }

        public IEnumerable<ConsumerEntry> RequestConsumers { get; set; }

        public IEnumerable<ConsumerEntry> MessageConsumers { get; set; }

        public IReadOnlyDictionary<Type, string> SendEndpoints { get; set; }

        public IReadOnlyDictionary<Type, string> RequestClientEndpoints { get; set; }
    }
}
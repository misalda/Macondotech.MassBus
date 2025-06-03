using System;
using System.Collections.Generic;

namespace MacondoTech.EnterpriseBus.Conventions
{
    public interface INamespaceResolver
    {
        TopologyMap BuildTopology();
    }
}
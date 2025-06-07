using System;
using System.Collections.Generic;

namespace CTM.EnterpriseBus.Conventions
{
    public interface INamespaceResolver
    {
        TopologyMap BuildTopology();
    }
}
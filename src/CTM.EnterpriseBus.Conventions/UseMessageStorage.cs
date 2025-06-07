using System;

namespace CTM.EnterpriseBus.Conventions
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class UseMessageStorageAttribute : Attribute
    {
         
    }
}

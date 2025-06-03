using System;

namespace MacondoTech.EnterpriseBus.Conventions
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class UseMessageStorageAttribute : Attribute
    {
         
    }
}

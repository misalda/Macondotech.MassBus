using System;

namespace CTM.EnterpriseBus.Conventions
{
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Class, AllowMultiple = false)]
    public class EndPointNameAttribute : Attribute
    {
        private string _name;
        public virtual string EndpointName
        {
            get { return _name; }
        }
        public EndPointNameAttribute(string name) 
        {
            _name = name;
        }
    }
}

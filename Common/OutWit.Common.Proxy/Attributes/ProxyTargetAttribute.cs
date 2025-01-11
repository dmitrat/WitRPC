using System;

namespace OutWit.Common.Proxy.Attributes
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ProxyTargetAttribute : Attribute
    {
        public ProxyTargetAttribute(string name = "")
        {
            Name = name;
        }

        public string Name { get; }
    }
}

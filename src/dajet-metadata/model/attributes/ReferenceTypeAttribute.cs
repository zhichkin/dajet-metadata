using System;
using System.Collections.Generic;
using System.Text;

namespace DaJet.Metadata.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ReferenceTypeAttribute : Attribute
    {
        private readonly Type _type = typeof(Guid);
        private readonly string _name = string.Empty;
        
        public ReferenceTypeAttribute() { }
        public ReferenceTypeAttribute(Type type) { _type = type; }
        public ReferenceTypeAttribute(string name) { _name = name; }
        public ReferenceTypeAttribute(Type type, string name)
        {
            _type = type;
            _name = name;
        }

        public Type Type { get { return _type; } }
        public string Name { get { return _name; } }
    }
}
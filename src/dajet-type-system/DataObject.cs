using System.Collections;
using System.Dynamic;

namespace DaJet.TypeSystem
{
    public sealed class DataObject : DynamicObject, IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> _data;
        public DataObject()
        {
            _data = new Dictionary<string, object>();
        }
        public DataObject(int capacity)
        {
            _data = new Dictionary<string, object>(capacity);
        }
        public DataObject(Dictionary<string, object> data)
        {
            _data = data is not null ? data : new Dictionary<string, object>();
        }
        public bool IsEmpty { get { return _data.Count == 0; } }
        public void SetValue(string name, object value)
        {
            if (!_data.TryAdd(name, value))
            {
                _data[name] = value;
            }
        }
        public bool TrySetValue(string name, object value)
        {
            if (!_data.ContainsKey(name))
            {
                return false;
            }

            _data[name] = value;
            
            return true;
        }
        public object GetValue(string name)
        {
            return _data[name];
        }
        public bool TryGetValue(string name, out object value)
        {
            return _data.TryGetValue(name, out value);
        }
        public void Clear()
        {
            _data.Clear();
        }

        #region "IENUMERABLE IMPLEMENTATION"
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }
        #endregion

        #region "DYNAMIC OBJECT IMPLEMENTATION"
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _data.Keys;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return TrySetValue(binder.Name, value);
        }
        public override bool TryGetMember(GetMemberBinder binder, out object value)
        {
            return _data.TryGetValue(binder.Name, out value);
        }
        #endregion
    }
}
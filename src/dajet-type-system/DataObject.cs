using System.Collections;
using System.Dynamic;

namespace DaJet.TypeSystem
{
    public sealed class DataObject : DynamicObject, IEnumerable<KeyValuePair<string, object>>, IEnumerator<KeyValuePair<string, object>>
    {
        private readonly List<string> _keys;
        private readonly Dictionary<string, object> _data;
        public DataObject()
        {
            _keys = new List<string>();
            _data = new Dictionary<string, object>();
        }
        public DataObject(int capacity)
        {
            _keys = new List<string>(capacity);
            _data = new Dictionary<string, object>(capacity);
        }
        public DataObject(Dictionary<string, object> data)
        {
            _data = data is not null ? data : new Dictionary<string, object>();

            _keys = data is not null ? new List<string>(data.Count) : new List<string>();

            foreach (string key in _data.Keys)
            {
                _keys.Add(key);
            }
        }
        public static implicit operator DataObject(Dictionary<string, object> value) => new(value);
        public static implicit operator Dictionary<string, object>(DataObject value) => value._data;
        public int Count { get { return _keys.Count; } }
        public bool IsEmpty { get { return _data.Count == 0; } }
        public object GetFirstValue()
        {
            if (_keys.Count > 0)
            {
                return GetValue(0);
            }

            return null;
        }
        public void SetValue(int ordinal, object value)
        {
            _data[_keys[ordinal]] = value;
        }
        public void SetValue(string name, object value)
        {
            if (_data.TryAdd(name, value))
            {
                _keys.Add(name);
            }
            else
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
        public string GetName(int ordinal)
        {
            return _keys[ordinal];
        }
        public object GetValue(int ordinal)
        {
            return _data[_keys[ordinal]];
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
            _keys.Clear();
        }

        #region "ENUMERABLE IMPLEMENTATION"
        private int _current = -1;
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        object IEnumerator.Current => Current;
        public void Reset() { _current = -1; }
        public void Dispose() { _current = -1; }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            _current = -1; return this;
        }
        public bool MoveNext()
        {
            if (_current < _keys.Count)
            {
                _current++;
            }

            return _current < _keys.Count;
        }
        public KeyValuePair<string, object> Current
        {
            get
            {
                string key = _keys[_current];

                return new KeyValuePair<string, object>(key, _data[key]);
            }
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
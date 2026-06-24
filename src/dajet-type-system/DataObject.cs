namespace DaJet.TypeSystem
{
    /// <summary>
    /// Универсальный объект представления данных, имеющий произвольное количество свойств, создаваемых или удаляемых программно.
    /// Экземпляры данного класса предназначены для представления данных запросов, объектов конфигураций баз данных, а также
    /// любых иных динамически изменяемых структур данных и переноса этих данных между программными компонентами.
    /// </summary>
    public sealed class DataObject : Dictionary<string, object>
    {
        public DataObject() : base() { }
        public DataObject(int capacity) : base(capacity) { }
        /// <summary>
        /// Replaces an existing property value or adds a new
        /// property to the object using the provided value.
        /// </summary>
        public void SetValue(string name, object value)
        {
            if (!TryAdd(name, value))
            {
                this[name] = value;
            }
        }
    }
}
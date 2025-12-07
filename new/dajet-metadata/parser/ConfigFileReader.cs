using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace DaJet.Metadata
{
    public static class ConfigFileToken
    {
        public const uint StartObject = uint.MinValue;
        public const uint Null = 1;
        public const uint Value = 2;
        public const uint String = 3;
        public const uint EndObject = uint.MaxValue;
    }
    internal static class CharBytes
    {
        internal const byte EF = 0xEF; // B(yte)
        internal const byte BB = 0xBB; // O(rder)
        internal const byte BF = 0xBF; // M(ark)
        internal const byte OpenBrace = (byte)'{';
        internal const byte CloseBrace = (byte)'}';
        internal const byte Comma = (byte)',';
        internal const byte Quote = (byte)'"';
        internal const byte CR = (byte)'\r';
        internal const byte LF = (byte)'\n';
        internal const byte BackSlash = (byte)'\\';
    }
    public ref struct ConfigFileReader
    {
        private int _consumed; // Номер следующего или количество уже прочитанных байт буфера
        private int _valueStart; // Начальный байт значения в буфере
        private int _valueEnd; // Конечный байт значения в буфере
        private int _element = -1; // Указатель на текущий/последний элемент вектора состояния
        private ConfigFileVector _vector = new(); // Логический адрес текущего значения
        private uint _token = ConfigFileToken.Null; // Текущий токен или начало файла
        private ReadOnlySpan<byte> _buffer; // Буфер входных данных (файл метаданных)
        private int _length = -1; // Указатель на текущий/последний элемент вектора поиска
        private ConfigFileVector _target = new(); // Логический адрес искомого значения
        public ConfigFileReader(ReadOnlySpan<byte> buffer)
        {
            if (buffer == ReadOnlySpan<byte>.Empty)
            {
                throw new InvalidOperationException();
            }

            _buffer = buffer;

            if (_buffer.Length >= 3)
            {
                if (_buffer[0] == CharBytes.EF && // B(yte)
                    _buffer[1] == CharBytes.BB && // O(rder)
                    _buffer[2] == CharBytes.BF)   // M(ark)
                {
                    _consumed = 3; // UTF-8
                }
            }
        }
        public readonly uint Token { get { return _token; } }
        public readonly int Consumed { get { return _consumed; } }
        public ref ConfigFileReader this[uint value]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _target[++_length] = value;

                return ref Unsafe.AsRef(in this);
            }
        }
        public ref ConfigFileReader this[ReadOnlySpan<uint> values]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                for (int i = 0; i < values.Length; i++)
                {
                    _target[++_length] = values[i];
                }

                return ref Unsafe.AsRef(in this);
            }
        }
        public string VectorAsString
        {
            get
            {
                StringBuilder vector = new(64);

                for (int element = 0; element <= _element; element++)
                {
                    uint value = _vector[element];

                    if (value == ConfigFileToken.StartObject) { vector.Append(' ').Append('{'); }
                    else if (value == ConfigFileToken.EndObject) { vector.Append(' ').Append('}'); }
                    else
                    {
                        vector.Append('[');
                        vector.Append(value);
                        vector.Append(']');
                    }
                }

                return vector.ToString();
            }
        }

        public readonly ReadOnlySpan<byte> GetBytes()
        {
            int length = _valueEnd - _valueStart;

            if (length < 1)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            if (_token != ConfigFileToken.String)
            {
                return _buffer[_valueStart.._valueEnd];
            }

            if (length < 2)
            {
                return ReadOnlySpan<byte>.Empty; // Пустая строка
            }

            int start = _valueStart + 1;
            length = _valueEnd - 1;

            return _buffer[start..length];
        }
        public readonly int GetChars(Span<char> buffer)
        {
            int length = _valueEnd - _valueStart;

            if (length < 1)
            {
                return 0; // Пустое значение
            }

            ReadOnlySpan<byte> value;

            if (_token != ConfigFileToken.String)
            {
                value = _buffer[_valueStart..Math.Min(length, buffer.Length)];

                return Encoding.UTF8.GetChars(value, buffer);
            }

            if (length < 2)
            {
                return 0; // Пустая строка
            }

            int start = _valueStart + 1;
            
            length = _valueEnd -1;

            value = _buffer[start..length];

            return Encoding.UTF8.GetChars(value, buffer);
        }

        private ReadOnlySpan<byte> ValueAsSpan
        {
            get
            {
                int length = _valueEnd - _valueStart;

                if (length < 1)
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                else
                {
                    return _buffer[_valueStart.._valueEnd];
                }
            }
        }
        public Guid ValueAsUuid
        {
            get
            {
                if (!Utf8Parser.TryParse(ValueAsSpan, out Guid uuid, out _))
                {
                    throw new FormatException();
                }
                return uuid;
            }
        }
        public int ValueAsNumber
        {
            get
            {
                if (!Utf8Parser.TryParse(ValueAsSpan, out int number, out _))
                {
                    throw new FormatException();
                }
                return number;
            }
        }
        public string ValueAsString
        {
            get
            {
                int length = _valueEnd - _valueStart;

                if (length < 1)
                {
                    return string.Empty;
                }

                ReadOnlySpan<byte> span = ValueAsSpan;

                if (span.StartsWith(CharBytes.Quote))
                {
                    span = span[1..(length - 1)];
                }
                else
                {
                    return Encoding.UTF8.GetString(span);
                }

                int index = span.IndexOf(CharBytes.Quote);

                if (index == -1) // В строке нет символов "" (экранированные кавычки)
                {
                    return Encoding.UTF8.GetString(span);
                }

                byte[] buffer = null;

                Span<byte> value = (span.Length > 512)
                    ? (buffer = ArrayPool<byte>.Shared.Rent(span.Length))
                    : stackalloc byte[1024];

                span[0..index].CopyTo(value);

                int postion = index;

                for (; index < span.Length; index++)
                {
                    value[postion] = span[index];
                    
                    postion++;
                    
                    if (span[index] == CharBytes.Quote)
                    {
                        index++;
                    }
                }

                string result = Encoding.UTF8.GetString(value[0..postion]);

                if (buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                return result;
            }
        }
        
        public Guid SeekUuid() => Seek() ? ValueAsUuid : Guid.Empty;
        public int SeekNumber() => Seek() ? ValueAsNumber : 0;
        public string SeekString() => Seek() ? ValueAsString : string.Empty;

        public void Dump(in StreamWriter writer)
        {
            while (Read())
            {
                string path = VectorAsString;

                if (_token == ConfigFileToken.StartObject)
                {
                    writer.WriteLine(path);
                }
                else if (_token == ConfigFileToken.EndObject)
                {
                    writer.WriteLine(path);
                }
                else if (_token == ConfigFileToken.Null)
                {
                    writer.WriteLine($"{path} NULL");
                }
                else if (_token == ConfigFileToken.String)
                {
                    writer.WriteLine($"{path} {ValueAsString}");
                }
                else
                {
                    writer.WriteLine($"{path} {ValueAsString}");
                }
            }
        }

        public bool Read()
        {
            ConsumeIgnoredChars();

            byte current;

            if (_consumed < _buffer.Length)
            {
                current = _buffer[_consumed];
            }
            else
            {
                return false; // Конец файла
            }

            switch (current)
            {
                case CharBytes.OpenBrace: StartObject(); break;
                case CharBytes.CloseBrace: EndObject(); break;
                case CharBytes.Comma: ProcessComma(); break;
                case CharBytes.Quote: ReadString(); break;
                default: ReadValue(); break;
            }

            return true;
        }
        private void ConsumeIgnoredChars()
        {
            ReadOnlySpan<byte> buffer = _buffer;

            while (_consumed < buffer.Length)
            {
                byte current = buffer[_consumed];

                if (current == CharBytes.CR || current == CharBytes.LF)
                {
                    _consumed++;
                }
                else
                {
                    break;
                }
            }
        }
        private void StartObject()
        {
            // Начало файла { или {{ или ,{

            if (_token == ConfigFileToken.StartObject) // {{
            {
                _vector[_element]++; // Прочитано значение следующего элемента вектора
            }

            _element++; // Открываем новый объект или элемент логического адреса
            _vector[_element] = ConfigFileToken.StartObject; // Значение нового элемента

            _consumed++; // Потребляем текущий байт из буфера (открывающую скобку) и переходим к следующему

            _token = ConfigFileToken.StartObject;
        }
        private void EndObject()
        {
            // {} или }} или <value>} или ,}

            if (_token == ConfigFileToken.StartObject) // {}
            {
                _vector[_element]++; // Прочитано значение следующего элемента вектора
                _token = ConfigFileToken.Null; // Прочитано значение NULL
                return; // Закрывающая скобка будет прочитана ещё раз
            }

            if (_token == ConfigFileToken.EndObject) // }}
            {
                // Обнуляем значение текущего элемента - это нужно для векторного поиска
                _vector[_element] = ConfigFileToken.StartObject;
                _element--; // Закрываем вложенный объект или текущий элемент логического адреса
            }

            _vector[_element] = ConfigFileToken.EndObject; // Значение текущего элемента

            _consumed++; // Потребляем текущий байт буфера (закрывающую скобку) и переходим к следующему

            _token = ConfigFileToken.EndObject;
        }
        private void ProcessComma()
        {
            // {, или }, или ,, или ,} или  ,{ или ," или ,<value>

            if (_token == ConfigFileToken.StartObject) // {,
            {
                _vector[_element]++; // Прочитано значение следующего элемента вектора
                _token = ConfigFileToken.Null; // Прочитано значение NULL
                return; // Запятая будет прочитана ещё раз
            }

            if (_token == ConfigFileToken.EndObject) // },
            {
                _vector[_element] = ConfigFileToken.StartObject; // Обнуляем значение текущего элемента
                _element--; // Закрываем вложенный объект или текущий элемент логического адреса
            }

            _consumed++; // Потребляем текущий байт буфера (запятую) и анализируем следующий

            ConsumeIgnoredChars();

            byte current;

            if (_consumed < _buffer.Length)
            {
                current = _buffer[_consumed];
            }
            else
            {
                throw new FormatException("Неожиданный конец файла");
            }

            if (current == CharBytes.Comma || current == CharBytes.CloseBrace) // ,, или ,}
            {
                _vector[_element]++; // Прочитано значение следующего элемента вектора
                _token = ConfigFileToken.Null; // Прочитано значение NULL
                return; // Запятая или закрывающая скобка будут прочитаны ещё раз
            }

            if (current == CharBytes.OpenBrace) // ,{
            {
                _vector[_element]++; // Прочитано значение следующего элемента вектора
                StartObject();
                return;
            }

            if (current == CharBytes.Quote) // ,"
            {
                ReadString();
            }
            else
            {
                ReadValue(); // ,<value>
            }
        }
        private void ReadValue()
        {
            // <value>, или <value>}

            ReadOnlySpan<byte> buffer = _buffer;

            int start = _consumed; // Запоминаем начальную позицию значения в буфере
            
            _consumed++; // Переходим к следующему байту буфера

            while (_consumed < buffer.Length)
            {
                byte current = buffer[_consumed];

                if (current == CharBytes.Comma || current == CharBytes.CloseBrace) // Конечная позиция значения в буфере
                {
                    _valueStart = start;
                    _valueEnd = _consumed;
                    _vector[_element]++; // Прочитано значение следующего элемента вектора
                    _token = ConfigFileToken.Value; // Прочитано числовое значение или UUID
                    return;
                }

                _consumed++; // Переходим к следующему байту буфера
            }

            throw new FormatException("Неожиданный конец файла");
        }
        private void ReadString()
        {
            // "<string>", или "<string>"}

            ReadOnlySpan<byte> buffer = _buffer;

            int start = _consumed; // Запоминаем начальную позицию значения в буфере

            _consumed++; // Переходим к следующему байту буфера

            while (_consumed < buffer.Length)
            {
                byte current = buffer[_consumed];

                if (current == CharBytes.Quote) // Возможно, что это конечная позиция значения в буфере
                {
                    _consumed++; // Переходим к следующему байту буфера

                    byte next; // Необходимо проверить следующий байт на экранирование кавычек

                    if (_consumed < buffer.Length)
                    {
                        next = buffer[_consumed];
                    }
                    else
                    {
                        throw new FormatException("Неожиданный конец файла");
                    }

                    if (next != CharBytes.Quote) // Это не экранирующая кавычка, а конечная позиция значения в буфере
                    {
                        _valueStart = start;
                        _valueEnd = _consumed;
                        _vector[_element]++; // Прочитано значение следующего элемента вектора
                        _token = ConfigFileToken.String; // Прочитано строковое значение
                        return;
                    }
                }

                _consumed++; // Переходим к следующему байту буфера
            }

            throw new FormatException("Неожиданный конец файла");
        }

        public bool Seek()
        {
            int result = Compare();

            if (result == 0)
            {
                _length = -1; return true; // the search vector is equal to the current
            }

            if (result > 0)
            {
                _length = -1; return false; // the search vector is not available
            }

            while (result < 0 && Read())
            {
                result = Compare();
            }

            _length = -1;

            return (result == 0); // the search vector is found, unavailable or outside the bounds of the file
        }
        private readonly int Compare()
        {
            int target = _length + 1;
            int vector = _element + 1;

            int length = Math.Min(vector, target);

            //THINK: return _vector[..length].SequenceCompareTo(_target[..length]);
            //THINK: Есть небольшой нюанс в сравнении длин массивов (см. исходники Microsoft)

            for (int element = 0; element < length; element++)
            {
                if (_vector[element] < _target[element])
                {
                    return -1; // current vector is less than target
                }
                else if (_vector[element] > _target[element])
                {
                    return 1; // current vector is greater than target
                }
            }

            if (vector < target)
            {
                return -1; // current vector is less than target
            }
            else if (vector > target)
            {
                return 1; // current vector is greater than target
            }

            return 0; // both vectors are equal
        }
    }
}
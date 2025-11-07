namespace DaJet
{
    public sealed class TypeDefinition
    {
        private int _dataType = 0;
        private void SetBit(int position, bool value)
        {
            if (value) { _dataType |= (1 << position); } else { _dataType &= ~(1 << position); }
        }
        private bool IsBitSet(int position)
        {
            return (_dataType & (1 << position)) == (1 << position);
        }

        #region "Квалификаторы для различных типов данных"
        
        ///<summary>Квалификатор: длина строки в символах. Неограниченная длина равна 0.</summary>
        public int StringLength { get; set; } = 10;
        ///<summary>
        ///Квалификатор: фиксированная (дополняется пробелами) или переменная длина строки.
        ///<br>
        ///Строка неограниченной длины (длина равна 0) всегда является переменной строкой.
        ///</br>
        ///</summary>
        public StringKind StringKind { get; set; } = StringKind.Variable;
        ///<summary>Квалификатор: определяет допустимое количество знаков после запятой.</summary>
        public int NumericScale { get; set; } = 0;
        ///<summary>Квалификатор: определяет разрядность числа (сумма знаков до и после запятой).</summary>
        public int NumericPrecision { get; set; } = 10;
        ///<summary>Квалификатор: определяет возможность использования отрицательных значений.</summary>
        public NumericKind NumericKind { get; set; } = NumericKind.CanBeNegative;
        ///<summary>Квалификатор: определяет используемые части даты.</summary>
        public DateTimePart DateTimePart { get; set; } = DateTimePart.Date;
        ///<summary>
        ///Код типа объекта метаданных (дискриминатор)
        ///<br>Используется для формирования имени таблицы СУБД, а также как</br>
        ///<br>значение поля RTRef составного типа данных в записях таблиц СУБД.</br>
        ///<br><b>Значение по умолчанию: 0</b> (допускает множественный ссылочный тип данных)</br>
        ///<br>Выполняет роль квалификатора ссылочного типа данных.</br>
        ///</summary>
        public int TypeCode { get; set; } = 0;

        #endregion

        public bool IsUndefined { get { return _dataType == (int)UnionTag.Undefined; } }
        public bool IsBoolean { get { return IsBitSet((int)UnionTag.Boolean); } set { SetBit((int)UnionTag.Boolean, value); } }
        public bool IsDecimal { get { return IsBitSet((int)UnionTag.Decimal); } set { SetBit((int)UnionTag.Decimal, value); } }
        public bool IsDateTime { get { return IsBitSet((int)UnionTag.DateTime); } set { SetBit((int)UnionTag.DateTime, value); } }
        public bool IsString { get { return IsBitSet((int)UnionTag.String); } set { SetBit((int)UnionTag.String, value); } }
        public bool IsBinary { get { return IsBitSet((int)UnionTag.Binary); } set { SetBit((int)UnionTag.Binary, value); } }
        public bool IsUuid { get { return IsBitSet((int)UnionTag.Uuid); } set { SetBit((int)UnionTag.Uuid, value); } }
        public bool IsEntity { get { return IsBitSet((int)UnionTag.Entity); } set { SetBit((int)UnionTag.Entity, value); } }
        public bool IsInteger { get { return IsBitSet((int)UnionTag.Integer); } set { SetBit((int)UnionTag.Integer, value); } }
        
        ///<summary>Метод проверяет является ли описание составным типом данных</summary>
        public bool IsUnion()
        {
            if (IsUuid || IsBinary)
            {
                return false; // УникальныйИдентификатор или ХранилищеЗначения
            }

            if (IsString && StringLength == 0)
            {
                return false; // Строка неограниченной длины не поддерживает составной тип данных!
            }

            int count = 0;
            if (IsBoolean) { count++; }
            if (IsDecimal) { count++; }
            if (IsDateTime) { count++; }
            if (IsString) { count++; }
            if (IsEntity) { count++; }
            if (count > 1)
            {
                return true;
            }

            if (IsEntity && TypeCode == 0)
            {
                return true;
            }

            return false;
        }
        public bool IsUnion(out bool canBeSimple, out bool canBeReference)
        {
            canBeSimple = false;
            canBeReference = false;

            if (IsUuid || IsBinary)
            {
                return false; // УникальныйИдентификатор или ХранилищеЗначения
            }

            if (IsString && StringLength == 0)
            {
                return false; // Строка неограниченной длины не поддерживает составной тип данных!
            }

            int count = 0;
            if (IsBoolean) { count++; }
            if (IsDecimal) { count++; }
            if (IsDateTime) { count++; }
            if (IsString) { count++; }
            if (count > 0)
            {
                canBeSimple = true;
            }

            if (IsEntity)
            {
                count++; canBeReference = true;
            }

            if (count > 1)
            {
                return true;
            }

            if (canBeReference && TypeCode == 0)
            {
                return true;
            }

            return false;
        }
    }
}
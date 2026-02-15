using DaJet.TypeSystem;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaJet.Metadata
{
    internal sealed class MetadataRegistry
    {
        ///<summary>
        ///Режим совместимости платформы 1С:Предприятие 8
        ///<br>(определяет доступный функционал базы данных)</br>
        ///</summary>
        internal int Version { get; set; }
        internal int YearOffset { get; set; }

        private uint _generic_extension_flags;
        private readonly Dictionary<int, Guid> _type_codes = new();
        private readonly Dictionary<Guid, MetadataObject> _registry = new();
        private readonly ConcurrentDictionary<Guid, Guid> _references = new();
        private readonly Dictionary<Guid, Guid> _defined_types = new();
        private readonly Dictionary<Guid, Guid> _characteristics = new();
        private readonly Dictionary<Guid, List<Guid>> _task_processes = new();
        private readonly Dictionary<Guid, List<Guid>> _register_recorders = new();
        private readonly Dictionary<string, string> _files = new();
        private readonly Dictionary<Guid, List<Guid>> _borrowed = new();
        private readonly Dictionary<string, Dictionary<string, Guid>> _names = new(14)
        {
            [MetadataNames.SharedProperty] = new Dictionary<string, Guid>(),
            [MetadataNames.Publication] = new Dictionary<string, Guid>(),
            [MetadataNames.DefinedType] = new Dictionary<string, Guid>(),
            [MetadataNames.Constant] = new Dictionary<string, Guid>(),
            [MetadataNames.Catalog] = new Dictionary<string, Guid>(),
            [MetadataNames.Document] = new Dictionary<string, Guid>(),
            [MetadataNames.Enumeration] = new Dictionary<string, Guid>(),
            [MetadataNames.Characteristic] = new Dictionary<string, Guid>(),
            [MetadataNames.Account] = new Dictionary<string, Guid>(),
            [MetadataNames.InformationRegister] = new Dictionary<string, Guid>(),
            [MetadataNames.AccumulationRegister] = new Dictionary<string, Guid>(),
            [MetadataNames.AccountingRegister] = new Dictionary<string, Guid>(),
            [MetadataNames.BusinessProcess] = new Dictionary<string, Guid>(),
            [MetadataNames.BusinessTask] = new Dictionary<string, Guid>()
        };
        internal List<Configuration> Configurations { get; } = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFileName(in string identifier, in string fileName)
        {
            _ = _files.TryAdd(identifier, fileName);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetFileName(in string identifier, [MaybeNullWhen(false)] out string fileName)
        {
            return _files.TryGetValue(identifier, out fileName);
        }

        #region "Инциализация реестра по данным DBNames"
        internal bool TryRegisterDbName(Guid uuid, int code, in string token)
        {
            // Токен главного объекта метаданных должен следовать первым в файле DBNames.
            // Это штатное поведение платформы 1С. Нарушение порядка следования - ошибка.

            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrNullRef(_registry, uuid);

            if (Unsafe.IsNullRef(ref entry)) // Ключ основного объекта не найден в реестре метаданных
            {
                if (token == MetadataToken.VT)
                {
                    TablePart tablePart = new(uuid); // Создаём основной объект метаданных
                    tablePart.AddDbName(code, token); // Устанавливаем код объекта
                    _registry.Add(uuid, tablePart); // Добавляем объект в реестр метаданных
                    return true; // Объект реестра метаданных обработан успешно
                }
                else if (token == MetadataToken.Fld)
                {
                    Property property = new(uuid); // Создаём основной объект метаданных
                    property.AddDbName(code, token); // Устанавливаем код объекта
                    _registry.Add(uuid, property); // Добавляем объект в реестр метаданных
                    return true; // Объект реестра метаданных обработан успешно
                }
                else
                {
                    // 0. Неизвестный или неподдерживаемый объект конфигурации.
                    // 1. Нарушен порядок основной-подчинённый, например, объект LineNo следует раньше VT.
                    // 2. Ошибка платформы 1С: в файле DBNames присутствует основной объект,
                    //    который отсутствует в главном списке объектов конфигурации (файл root).
                    return false; // Объект метаданных добавляется в список постобработки
                }
            }

            if (entry is null) // Ключ объекта найден, но его значение равно null (критическая ошибка библиотеки)
            {
                throw new InvalidOperationException($"Ошибка загрузки файла DBNames: [{token}] {{{code}:{uuid}}}");
            }

            entry.AddDbName(code, token); // Устанавливаем код объекта (основного или подчинённого)

            if (Configurator.IsReferenceTypeToken(token))
            {
                _ = _type_codes.TryAdd(code, uuid); // Таблица разрешения кодов ссылочных объектов
            }

            return true; // Объект реестра метаданных обработан успешно
        }
        internal void RegisterMissedDbName(Guid uuid, int code, string token)
        {
            //NOTE: Сюда в принципе попадать не планируется ...

            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrNullRef(_registry, uuid);

            if (Unsafe.IsNullRef(ref entry)) // Ключ объекта не найден в реестре метаданных
            {
                // Ошибка платформы 1С: подчинённый объект есть, а его основного нет.
                // Выявлено однажды в конфигурации УНФ - InfoRgOpt ¯\_(ツ)_/¯
                // Соответствующая служебная таблица в СУБД присутствовала, а основная нет.
                return;
            }

            if (entry is null) // Ключ объекта найден, но его значение равно null (критическая ошибка библиотеки)
            {
                // В файле DBNames-Ext-1 может располагаться подчинённый объект, например, ReferenceChngR,
                // а его основной объект Reference может при этом находиться в одном из файлов DBNames-Ext-x.
                throw new InvalidOperationException($"Ошибка загрузки файла DBNames: [{token}] {{{code}:{uuid}}}");
            }

            // Исправление нештатного поведения платформы 1С: порядок токенов основной-подчинённый нарушен

            entry.AddDbName(code, token); // Подчинённый объект метаданных добавляется в свой основной объект
        }
        #endregion

        #region "Методы инциализации реестра метаданных"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntry(Guid uuid, in MetadataObject entry)
        {
            // Безусловное добавление объектов в реестр метаданных, кроме VT и Fld
            // Выполняется загрузчиком основной конфигурации до заполнения реестра метаданных
            // Класс MetadataLoader, метод GetMetadataRegistry

            _ = _registry.TryAdd(uuid, entry);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddReference(Guid uuid, Guid reference)
        {
            _ = _references.TryAdd(reference, uuid);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddMetadataName(in string type, in string name, Guid uuid)
        {
            if (_names.TryGetValue(type, out Dictionary<string, Guid> names))
            {
                _ = names.TryAdd(name, uuid);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddBorrowed(Guid parent, Guid extension)
        {
            //NOTE: Важно! Данный код выполняется однопоточно инициализатором реестра
            //NOTE: метаданных для объектов одного и того же типа, например, справочников

            if (_borrowed.TryGetValue(parent, out List<Guid> borrowed))
            {
                borrowed.Add(extension);
            }
            else
            {
                _borrowed.Add(parent, new List<Guid>() { extension });
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddDefinedType(Guid uuid, Guid reference)
        {
            _ = _defined_types.TryAdd(reference, uuid);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddCharacteristic(Guid uuid, Guid characteristic)
        {
            _ = _characteristics.TryAdd(characteristic, uuid);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddRecorderToRegister(Guid document, Guid register)
        {
            ref List<Guid> recorders = ref CollectionsMarshal.GetValueRefOrAddDefault(_register_recorders, register, out bool exists);

            if (exists)
            {
                recorders.Add(document);
            }
            else
            {
                recorders = new List<Guid>(1) { document };
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddBusinessProcessToTask(Guid task, Guid process)
        {
            ref List<Guid> processes = ref CollectionsMarshal.GetValueRefOrAddDefault(_task_processes, task, out bool exists);

            if (exists)
            {
                processes.Add(process);
            }
            else
            {
                processes = new List<Guid>(1) { process };
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetBorrowed(Guid parent, out List<Guid> borrowed)
        {
            return _borrowed.TryGetValue(parent, out borrowed);
        }
        internal bool TryGetReference(Guid reference, [MaybeNullWhen(false)] out MetadataObject entry)
        {
            if (!_references.TryGetValue(reference, out Guid uuid))
            {
                entry = null;
                return false;
            }

            return TryGetEntry(uuid, out entry);
        }
        internal bool TryGetDefinedType(Guid reference, [MaybeNullWhen(false)] out DefinedType entry)
        {
            if (!_defined_types.TryGetValue(reference, out Guid uuid))
            {
                entry = null;
                return false;
            }

            return TryGetEntry(uuid, out entry);
        }
        internal bool TryGetCharacteristic(Guid reference, [MaybeNullWhen(false)] out Characteristic entry)
        {
            if (!_characteristics.TryGetValue(reference, out Guid uuid))
            {
                entry = null;
                return false;
            }

            return TryGetEntry(uuid, out entry);
        }
        internal bool TryGetBusinessProcesses(Guid task, [MaybeNullWhen(false)] out List<Guid> processes)
        {
            return _task_processes.TryGetValue(task, out processes);
        }
        internal bool TryGetRegisterRecorders(Guid register, [MaybeNullWhen(false)] out List<Guid> recorders)
        {
            return _register_recorders.TryGetValue(register, out recorders);
        }

        internal int GetTypeCode(Guid uuid)
        {
            if (!_registry.TryGetValue(uuid, out MetadataObject entry))
            {
                return -1;
            }

            return entry.Code;
        }
        internal int GetGenericTypeCode(Guid generic)
        {
            string name = ReferenceType.GetMetadataName(generic);

            if (!_names.TryGetValue(name, out Dictionary<string, Guid> items))
            {
                return -1; // Нет ни одного объекта метаданных данного типа
            }

            if (items is null || items.Count == 0)
            {
                return -1; // Нет ни одного объекта метаданных данного типа
            }

            if (items.Count == 1) // Единственный объект метаданных общего типа
            {
                Guid uuid = items.Values.FirstOrDefault();

                if (TryGetEntry(uuid, out MetadataObject entry))
                {
                    return entry.Code;
                }
            }

            return 0; // Количество объектов данного типа больше 1
        }
        internal int GetGenericTypeCode(in List<Guid> generics)
        {
            int typeCode = -1; // Нет ни одного конкретного ссылочного типа

            foreach (Guid generic in generics)
            {
                typeCode = GetGenericTypeCode(generic);

                if (typeCode == 0) // Объектов метаданных больше 1
                {
                    return 0; // Составной ссылочный тип
                }
            }

            return typeCode;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetGenericExtensionFlag(GenericExtensionFlags flag)
        {
            _ = Interlocked.Or(ref _generic_extension_flags, (uint)flag);
        }
        internal bool HasGenericExtensionFlag(Guid generic)
        {
            if (generic == ReferenceType.AnyReference)
            {
                return _generic_extension_flags != (uint)GenericExtensionFlags.None;
            }
            
            GenericExtensionFlags flag = ReferenceType.GetGenericExtensionFlag(generic);

            return (_generic_extension_flags & (uint)flag) == (uint)flag;
        }

        internal bool TryGetEntry(int code, [MaybeNullWhen(false)] out MetadataObject entry)
        {
            if (!_type_codes.TryGetValue(code, out Guid uuid))
            {
                entry = null;
                return false;
            }

            return TryGetEntry(uuid, out entry);
        }
        internal bool TryGetEntry(Guid uuid, [MaybeNullWhen(false)] out MetadataObject entry)
        {
            return _registry.TryGetValue(uuid, out entry);
        }
        internal bool TryGetEntry<T>(Guid uuid, [MaybeNullWhen(false)] out T value) where T : MetadataObject
        {
            if (!_registry.TryGetValue(uuid, out MetadataObject entry))
            {
                value = null;
                return false;
            }

            value = entry as T;

            return value is not null;
        }
        internal bool TryGetEntry<T>(in string type, in string name, [MaybeNullWhen(false)] out T value) where T : MetadataObject
        {
            if (!_names.TryGetValue(type, out Dictionary<string, Guid> items))
            {
                value = null;
                return false;
            }

            if (!items.TryGetValue(name, out Guid uuid))
            {
                value = null;
                return false;
            }

            if (!_registry.TryGetValue(uuid, out MetadataObject entry))
            {
                value = null;
                return false; // Какой-то неизвестный Guid ...
            }

            value = entry as T;

            return value is not null;
        }

        internal IEnumerable<T> GetMetadataObjects<T>() where T : MetadataObject
        {
            string name = MetadataLookup.GetMetadataName(typeof(T));

            if (string.IsNullOrEmpty(name))
            {
                yield break;
            }

            if (!_names.TryGetValue(name, out Dictionary<string, Guid> items))
            {
                yield break;
            }

            foreach (KeyValuePair<string, Guid> item in items)
            {
                if (TryGetEntry(item.Value, out T entry))
                {
                    yield return entry;
                }
            }
        }
        internal IEnumerable<MetadataObject> GetMetadataObjects(string type)
        {
            if (!_names.TryGetValue(type, out Dictionary<string, Guid> items))
            {
                yield break;
            }

            foreach (KeyValuePair<string, Guid> item in items)
            {
                if (TryGetEntry(item.Value, out MetadataObject entry))
                {
                    yield return entry;
                }
            }
        }

        internal bool TryGetMetadataNames(in string type, out Dictionary<string, Guid> items)
        {
            return _names.TryGetValue(type, out items);
        }

        internal List<string> ResolveReferences(in List<Guid> references)
        {
            List<string> types = new();

            if (references is null || references.Count == 0)
            {
                return types;
            }

            for (int i = 0; i < references.Count; i++)
            {
                Guid reference = references[i];

                if (i == 0) // Единственно допустимая ссылка данного типа
                {
                    if (TryGetDefinedType(reference, out DefinedType defined))
                    {
                        types.Add(string.Format("ОпределяемыйТип.{0}", defined.Name)); break;
                    }

                    if (TryGetCharacteristic(reference, out Characteristic characteristic))
                    {
                        types.Add(string.Format("Характеристика.{0}", characteristic.Name)); break;
                    }
                }

                // Конкретный ссылочный тип

                if (TryGetReference(reference, out MetadataObject entry))
                {
                    types.Add(entry.ToString());
                }
                else // Общий ссылочный тип
                {
                    if (reference == ReferenceType.AnyReference)
                    {
                        types.Add("ЛюбаяСсылка");
                    }
                    else
                    {
                        types.Add(string.Format("{0}Ссылка", ReferenceType.GetMetadataName(reference)));
                    }
                }
            }

            return types;
        }

        internal void UpdateEntry(Guid uuid)
        {
            if (!_registry.TryGetValue(uuid, out MetadataObject entry))
            {
                return; // Какой-то неизвестный Guid ...
            }

            bool lockTaken = false;

            try
            {
                Monitor.Enter(entry, ref lockTaken);

                //TODO: Update metadata entry
                // ??? parser.Parse(in loader, in entry);
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(entry);
                }
            }
        }
    }
}
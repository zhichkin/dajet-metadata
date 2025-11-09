using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaJet
{
    internal sealed class MetadataRegistry
    {
        ///<summary>
        ///Режим совместимости платформы 1С:Предприятие 8,
        ///<br>согласно которому сконфигурирована база данных</br>
        ///</summary>
        internal int CompatibilityVersion { get; set; }

        private static readonly FrozenSet<string> SupportedTokens = FrozenSet.ToFrozenSet(
        [
            MetadataToken.VT,
            MetadataToken.LineNo,
            MetadataToken.Fld,
            MetadataToken.Enum,
            MetadataToken.Chrc,
            MetadataToken.Node,
            MetadataToken.Const,
            MetadataToken.Document,
            MetadataToken.Reference,
            MetadataToken.BPr,
            MetadataToken.Task,
            MetadataToken.BPrPoints,
            MetadataToken.InfoRg,
            MetadataToken.InfoRgOpt,
            MetadataToken.InfoRgSF,
            MetadataToken.InfoRgSL,
            MetadataToken.AccumRg,
            MetadataToken.AccumRgT,
            MetadataToken.AccumRgOpt,
            MetadataToken.Acc,
            MetadataToken.AccRg,
            MetadataToken.ExtDim,
            MetadataToken.AccRgED,
            MetadataToken.AccChngR,
            MetadataToken.AccRgChngR,
            MetadataToken.AccumRgChngR,
            MetadataToken.BPrChngR,
            MetadataToken.TaskChngR,
            MetadataToken.ReferenceChngR,
            MetadataToken.ChrcChngR,
            MetadataToken.ConstChngR,
            MetadataToken.DocumentChngR,
            MetadataToken.InfoRgChngR
        ], StringComparer.Ordinal);
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SupportedTokensLookup = SupportedTokens.GetAlternateLookup<ReadOnlySpan<char>>();

        private static readonly FrozenSet<string> ReferenceTypeTokens = FrozenSet.ToFrozenSet(
        [
            MetadataToken.Acc,
            MetadataToken.Enum,
            MetadataToken.Chrc,
            MetadataToken.Node,
            MetadataToken.BPr,
            MetadataToken.Task,
            MetadataToken.Document,
            MetadataToken.Reference
        ], StringComparer.Ordinal);
        private static readonly FrozenDictionary<string, Func<Guid, int, string, MetadataObject>> MainEntryTokens = CreateMainEntryFactoryLookup();
        private static FrozenDictionary<string, Func<Guid, int, string, MetadataObject>> CreateMainEntryFactoryLookup()
        {
            List<KeyValuePair<string, Func<Guid, int, string, MetadataObject>>> list =
            [
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.VT, TablePart.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Fld, Property.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Acc, Account.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Enum, Enumeration.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Chrc, Characteristic.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Node, Publication.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.BPr, BusinessProcess.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Task, BusinessTask.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Const, Constant.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Document, Document.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.Reference, Catalog.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.AccRg, AccountingRegister.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.InfoRg, InformationRegister.Create),
                new KeyValuePair<string, Func<Guid, int, string, MetadataObject>>(MetadataToken.AccumRg, AccumulationRegister.Create)
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }

        private readonly Dictionary<Guid, MetadataObject> _registry = new();
        private readonly Dictionary<int, Guid> _reference_type_codes = new();
        private readonly Dictionary<Guid, Guid> _defined_types = new();
        private readonly Dictionary<Guid, Guid> _characteristics = new();
        private readonly Dictionary<Guid, List<Guid>> _register_recorders = new();
        private readonly ConcurrentDictionary<Guid, Guid> _references = new();
        private readonly Dictionary<string, Dictionary<string, Guid>> _names = new(14)
        {
            [MetadataName.SharedProperty] = new Dictionary<string, Guid>(),
            [MetadataName.Publication] = new Dictionary<string, Guid>(),
            [MetadataName.DefinedType] = new Dictionary<string, Guid>(),
            [MetadataName.Constant] = new Dictionary<string, Guid>(),
            [MetadataName.Catalog] = new Dictionary<string, Guid>(),
            [MetadataName.Document] = new Dictionary<string, Guid>(),
            [MetadataName.Enumeration] = new Dictionary<string, Guid>(),
            [MetadataName.Characteristic] = new Dictionary<string, Guid>(),
            [MetadataName.Account] = new Dictionary<string, Guid>(),
            [MetadataName.InformationRegister] = new Dictionary<string, Guid>(),
            [MetadataName.AccumulationRegister] = new Dictionary<string, Guid>(),
            [MetadataName.AccountingRegister] = new Dictionary<string, Guid>(),
            [MetadataName.BusinessProcess] = new Dictionary<string, Guid>(),
            [MetadataName.BusinessTask] = new Dictionary<string, Guid>()
        };

        internal void EnsureCapacity(in Dictionary<Guid, Guid[]> metadata)
        {
            //_registry.EnsureCapacity(98918);

            // _characteristics = 24
            // _defined_types = 660
            // _references = 4021
            // _reference_type_codes = 4021
            // _registry = 98918

            // _names

            //if (metadata.TryGetValue(MetadataType.SharedProperty, out Guid[] items))
            //{
            //    if (_names.TryGetValue(MetadataName.SharedProperty, out Dictionary<string, Guid> names))
            //    {
            //        names.EnsureCapacity(items.Length);
            //    }
            //}
        }

        #region "Методы инциализации реестра метаданных"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntry(Guid uuid, in MetadataObject entry)
        {
            // Безусловное добавление объектов "ОбщийРеквизит" и "ОпределяемыйТип"
            // Выполняется загрузчиком до заполнения реестра метаданных
            // Класс MetadataLoader, метод GetMetadataRegistry

            _ = _registry.TryAdd(uuid, entry);
        }
        internal bool TryAddDbName(Guid uuid, int code, in string token)
        {
            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_registry, uuid, out bool exists);

            if (!exists) // Главные объекты метаданных
            {
                // Токен главного объекта метаданных должен следовать первым в файле DBNames.
                // Это штатное поведение платформы 1С. Нарушение порядка следования - ошибка.

                if (MainEntryTokens.TryGetValue(token, out Func<Guid, int, string, MetadataObject> factory))
                {
                    entry = factory(uuid, code, token); // Создаём главный объект метаданных

                    if (ReferenceTypeTokens.TryGetValue(token, out _))
                    {
                        _ = _reference_type_codes.TryAdd(code, uuid); // Таблица разрешения ссылок
                    }
                }
                else // Исправление возможной ошибки платформы 1С: порядок токенов главный-служебный нарушен
                {
                    return false; // Служебный объект метаданных добавляется в список постобработки
                }
            }
            else // Служебные объекты метаданных
            {
                if (entry is not null) // Главный объект уже добавлен: штатное поведение платформы 1С
                {
                    entry.AddDbName(code, token); // Служебный объект метаданных добавляется в свой главный объект
                }
                else // Исправление возможной ошибки платформы 1С: порядок токенов главный-служебный нарушен
                {
                    if (MainEntryTokens.TryGetValue(token, out Func<Guid, int, string, MetadataObject> factory))
                    {
                        entry = factory(uuid, code, token); // Создаём главный объект метаданных

                        if (ReferenceTypeTokens.TryGetValue(token, out _))
                        {
                            _ = _reference_type_codes.TryAdd(code, uuid); // Таблица разрешения ссылок
                        }
                    }
                    else
                    {
                        return false; // Служебный объект метаданных добавляется в список постобработки
                    }
                }
            }

            return true; // Объект реестра метаданных добавлен успешно
        }
        internal void AddMissedDbName(Guid uuid, int code, string token)
        {
            //NOTE: Сюда в принципе попадать не планируется ...

            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrNullRef(_registry, uuid);

            if (Unsafe.IsNullRef(ref entry))
            {
                return; // Ключ объекта не найден в реестре метаданных
            }

            if (entry is null) // Ключ объекта найден, но его значение равно null
            {
                // Ошибка платформы 1С: служебный объект есть, а главного - нет
                // Выявлено однажды в конфигурации УНФ - InfoRgOpt ¯\_(ツ)_/¯
                // Соответствующая служебная таблица в СУБД присутствовала, а главная - нет

                _ = _registry.Remove(uuid);

                return;
            }

            // Исправление возможной ошибки платформы 1С: порядок токенов главный-служебный нарушен

            entry.AddDbName(code, token); // Служебный объект метаданных добавляется в свой главный объект
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddReference(Guid uuid, Guid reference)
        {
            _ = _references.TryAdd(reference, uuid);
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
        internal void AddMetadataName(in string type, in string name, Guid uuid)
        {
            if (_names.TryGetValue(type, out Dictionary<string, Guid> names))
            {
                _ = names.TryAdd(name, uuid);
            }
        }
        #endregion

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

        internal int GetTypeCode(Guid uuid)
        {
            if (!_registry.TryGetValue(uuid, out MetadataObject entry))
            {
                return -1;
            }

            if (entry is not DatabaseObject dbo)
            {
                return -1;
            }

            return dbo.TypeCode;
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
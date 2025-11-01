using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaJet
{
    internal sealed class MetadataRegistry
    {
        private static readonly FrozenSet<string> SupportedTokens = FrozenSet.ToFrozenSet(new[]
        {
            MetadataToken.VT,
            MetadataToken.LineNo,
            MetadataToken.Fld,
            MetadataToken.ChngR,
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
            MetadataToken.AccRgED
        }, StringComparer.Ordinal);
        private static readonly FrozenSet<string> ReferenceTypeTokens = FrozenSet.ToFrozenSet(new[]
        {
            MetadataToken.Acc,
            MetadataToken.Enum,
            MetadataToken.Chrc,
            MetadataToken.Node,
            MetadataToken.BPr,
            MetadataToken.Task,
            MetadataToken.Document,
            MetadataToken.Reference
        }, StringComparer.Ordinal);
        private static readonly FrozenDictionary<string, Func<Guid, MetadataObject>> MainEntryTokens = CreateMainEntryFactoryLookup();
        private static FrozenDictionary<string, Func<Guid, MetadataObject>> CreateMainEntryFactoryLookup()
        {
            List<KeyValuePair<string, Func<Guid, MetadataObject>>> list =
            [
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.VT, static uuid => new TablePart(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Fld, static uuid => new MetadataProperty(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Acc, static uuid => new Account(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Enum, static uuid => new Enumeration(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Chrc, static uuid => new Characteristic(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Node, static uuid => new Publication(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.BPr, static uuid => new BusinessProcess(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Task, static uuid => new BusinessTask(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Const, static uuid => new Constant(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Document, static uuid => new Document(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Reference, static uuid => new Catalog(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.AccRg, static uuid => new AccountingRegister(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.InfoRg, static uuid => new InformationRegister(uuid)),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.AccumRg, static uuid => new AccumulationRegister(uuid))
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }

        private readonly Dictionary<Guid, MetadataObject> _registry = new();
        private readonly Dictionary<int, Guid> _reference_type_codes = new();
        private readonly Dictionary<Guid, Guid> _defined_types = new();
        private readonly Dictionary<Guid, Guid> _characteristics = new();
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

        #region "Методы инциализации реестра метаданных"
        internal bool TryAddDbName(Guid uuid, int code, string name)
        {
            if (!SupportedTokens.TryGetValue(name, out string token)) // Get interned string
            {
                return true; // Unsupported token
            }

            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_registry, uuid, out bool exists);

            if (!exists)
            {
                if (MainEntryTokens.TryGetValue(token, out Func<Guid, MetadataObject> factory))
                {
                    entry = factory(uuid); // main metadata objects
                }
                else
                {
                    return false; // dependent metadata objects - should be added into main entry
                }
            }

            entry.DbNames.Add(new DbName(code, token));

            if (ReferenceTypeTokens.TryGetValue(token, out _))
            {
                _ = _reference_type_codes.TryAdd(code, uuid);
            }

            return true;
        }
        internal void AddMissedDbName(Guid uuid, int code, string name)
        {
            ref MetadataObject entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_registry, uuid, out bool exists);

            if (exists)
            {
                if (SupportedTokens.TryGetValue(name, out string token)) // Get interned string
                {
                    entry.DbNames.Add(new DbName(code, token)); // add dependent metadata object into it's main
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntry(Guid uuid, in MetadataObject entry)
        {
            _ = _registry.TryAdd(uuid, entry);
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
            if (_registry.TryGetValue(register, out MetadataObject entry))
            {
                if (entry is Register metadata)
                {
                    metadata.Recorders.Add(document);
                }
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
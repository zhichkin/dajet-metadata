using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata.Parsers
{
    public sealed class MDObject
    {
        public List<object> Values { get; } = new List<object>();
    }
    public enum DiffKind { None, Insert, Update, Delete }
    public sealed class DiffObject
    {
        public string Path { get; set; }
        public DiffKind DiffKind { get; set; }
        public object SourceValue { get; set; }
        public object TargetValue { get; set; }
        public List<DiffObject> DiffObjects { get; } = new List<DiffObject>();
    }
    public static class MDObjectParser
    {
        public static MDObject Parse(StreamReader stream)
        {
            return ParseObject(stream, null);
        }
        private static MDObject ParseObject(StreamReader stream, MDObject parent)
        {
            char c;
            string value = null;
            MDObject mdo = null;
            while (!stream.EndOfStream)
            {
                do
                {
                    c = (char)stream.Read();
                }
                while (c == '\r' || c == '\n');

                if (c == '{') // start of object
                {
                    mdo = new MDObject();
                    ParseObject(stream, mdo);
                    if (parent != null) // this is child object
                    {
                        parent.Values.Add(mdo);
                    }
                }
                else if (c == '}') // end of object
                {
                    if (value != null)
                    {
                        parent.Values.Add(value);
                    }
                    return mdo; 
                }
                else if (c == '"') // start of string value
                {
                    value = string.Empty;
                    while (!stream.EndOfStream)
                    {
                        c = (char)stream.Read();
                        if (c == '"') // might be end of string
                        {
                            c = (char)stream.Read();
                            if (c == '"') // double quotes - this is not the end
                            {
                                value += c;
                            }
                            else // this is the end
                            {
                                parent.Values.Add(value);
                                value = null;
                                if (c == '}') // end of object
                                {
                                    return mdo;
                                }
                                break;
                            }
                        }
                        else
                        {
                            value += c;
                        }
                    }
                }
                else if (c == ',') // end of value
                {
                    if (value != null)
                    {
                        parent.Values.Add(value);
                        value = null;
                    }
                }
                else // number or uuid value
                {
                    if (value == null)
                    {
                        value = c.ToString();
                    }
                    else
                    {
                        value += c;
                    }
                }
            }
            return mdo;
        }
        public static DiffObject CompareObjects(MDObject source, MDObject target)
        {
            DiffObject diff = new DiffObject()
            {
                Path = string.Empty,
                SourceValue = source,
                TargetValue = target,
                DiffKind = DiffKind.None
            };

            CompareObjects(source, target, diff);

            return diff;
        }
        private static void CompareObjects(MDObject source, MDObject target, DiffObject diff)
        {
            int source_count = source.Values.Count;
            int target_count = target.Values.Count;
            int count = Math.Min(source_count, target_count);

            MDObject mdSource;
            MDObject mdTarget;
            for (int i = 0; i < count; i++) // update
            {
                mdSource = source.Values[i] as MDObject;
                mdTarget = target.Values[i] as MDObject;

                if (mdSource != null && mdTarget != null)
                {
                    DiffObject newDiff = CreateDiff(diff, DiffKind.Update, mdSource, mdTarget, i);
                    CompareObjects(mdSource, mdTarget, newDiff);
                    if (newDiff.DiffObjects.Count > 0)
                    {
                        diff.DiffObjects.Add(newDiff);
                    }
                }
                else if (mdSource != null || mdTarget != null)
                {
                    diff.DiffObjects.Add(
                            CreateDiff(
                                diff, DiffKind.Update, source.Values[i], target.Values[i], i));
                }
                else
                {
                    if ((string)source.Values[i] != (string)target.Values[i])
                    {
                        diff.DiffObjects.Add(
                            CreateDiff(
                                diff, DiffKind.Update, source.Values[i], target.Values[i], i));
                    }
                }
            }

            if (source_count > target_count) // delete
            {
                for (int i = count; i < source_count; i++)
                {
                    diff.DiffObjects.Add(
                        CreateDiff(
                            diff, DiffKind.Delete, source.Values[i], null, i));
                }
            }
            else if (target_count > source_count) // insert
            {
                for (int i = count; i < target_count; i++)
                {
                    diff.DiffObjects.Add(
                        CreateDiff(
                            diff, DiffKind.Insert, null, target.Values[i], i));
                }
            }
        }
        private static DiffObject CreateDiff(DiffObject parent, DiffKind kind, object source, object target, int path)
        {
            return new DiffObject()
            {
                Path = parent.Path + (string.IsNullOrEmpty(parent.Path) ? string.Empty : ".") + path.ToString(),
                SourceValue = source,
                TargetValue = target,
                DiffKind = kind
            };
        }
    }
}
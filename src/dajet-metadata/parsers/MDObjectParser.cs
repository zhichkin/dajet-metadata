using System.Collections.Generic;
using System.IO;
using System;

namespace DaJet.Metadata.Parsers
{
    public sealed class MDObject
    {
        public List<object> Values { get; } = new List<object>();
    }
    public static class MDObjectParser
    {
        public static MDObject Parse(StreamReader stream)
        {
            MDObject mdObject = new MDObject();
            ParseObject(stream, mdObject, 0);
            return mdObject;
        }
        private static void ParseObject(StreamReader stream, MDObject parent, int level)
        {
            char c;
            string value = null;
            while (!stream.EndOfStream)
            {
                do
                {
                    c = (char)stream.Read();
                }
                while (c == '\r' || c == '\n');

                if (c == '{') // start of object
                {
                    if (level == 0) // this is root object
                    {
                        ParseObject(stream, parent, level + 1);
                    }
                    else
                    {
                        MDObject child = new MDObject();
                        ParseObject(stream, child, level + 1);
                        parent.Values.Add(child);
                    }
                }
                else if (c == '}') // end of object
                {
                    if (value != null)
                    {
                        parent.Values.Add(value);
                    }
                    return; 
                }
                else if (c == ',') // end of value
                {
                    if (value != null)
                    {
                        parent.Values.Add(value);
                        value = null;
                    }
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
                                    return;
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
        }
    }
}
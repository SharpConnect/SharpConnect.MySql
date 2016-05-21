//MIT 2015, brezza92, EngineKit and contributors
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.LiquidData
{
    class LqArray : List<object>, LiquidArray
    {
        public void AddItem(object item)
        {
            Add(item);
        }
        public IEnumerable<object> GetIterForward()
        {
            foreach (object obj in this)
            {
                yield return obj;
            }
        }
    }
    static class LiquidElementHelper
    {
        public static LiquidElement CreateXmlElementForDynamicObject(LiquidDoc doc)
        {
            return new LqElement("!j", null);
        }
    }

    class LqElement : LiquidElement
    {
        //xml-like element

        string _name;
        int _nameIndex;
        LiquidDoc _owner;

        List<LiquidElement> _childNodes;
        Dictionary<string, LiquidAttribute> _attributeDic01 = new Dictionary<string, LiquidAttribute>();

        public LqElement(string elementName, LiquidDoc ownerdoc)
        {
            _name = elementName;
            _owner = ownerdoc;
        }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public LiquidDoc OwnerDocument
        {
            get
            {
                return _owner;
            }
        }
        public bool HasOwnerDocument
        {
            get
            {
                return _owner != null;
            }
        }
        public int ChildCount
        {
            get
            {
                if (_childNodes == null)
                {
                    return 0;
                }
                else
                {
                    return _childNodes.Count;
                }
            }
        }
        public object GetChild(int index)
        {
            return _childNodes[index];
        }
        public int NameIndex
        {
            get
            {
                return _nameIndex;
            }
        }
        public IEnumerable<LiquidAttribute> GetAttributeIterForward()
        {
            if (_attributeDic01 != null)
            {
                foreach (LiquidAttribute attr in this._attributeDic01.Values)
                {
                    yield return attr;
                }
            }
        }
        public void AppendChild(LiquidElement element)
        {
            if (_childNodes == null)
            {
                _childNodes = new List<LiquidElement>();
            }
            _childNodes.Add(element);

        }
        public void RemoveAttribute(LiquidAttribute attr)
        {
            _attributeDic01.Remove(attr.Name);
        }
        public void AppendAttribute(LiquidAttribute attr)
        {

            _attributeDic01.Add(attr.Name, attr);

        }
        public LiquidAttribute AppendAttribute(string key, object value)
        {
            var attr = new LqAttribute(key, value);
            _attributeDic01.Add(key, attr);
            return attr;
        }

        public object GetAttributeValue(string key)
        {
            LiquidAttribute found = GetAttributeElement(key);
            if (found != null)
            {
                return found.Value;
            }
            else
            {
                return null;
            }
        }
        public LiquidAttribute GetAttributeElement(string key)
        {
            LiquidAttribute existing;
            _attributeDic01.TryGetValue(key, out existing);
            return existing;
        }

    }
    class LqAttribute : LiquidAttribute
    {
        int _localNameIndex;
        public LqAttribute()
        {
        }
        public LqAttribute(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name
        {
            get;
            set;
        }
        public object Value
        {
            get;
            set;
        }
        public int AttributeLocalNameIndex
        {
            get
            {
                return _localNameIndex;
            }
        }
        public override string ToString()
        {
            return Name + ":" + Value;
        }
    }

    public static class LiquidExtensionMethods
    {

        public static string GetAttributeValueAsString(this LiquidElement lqElement, string attrName)
        {
            return lqElement.GetAttributeValue(attrName) as string;
        }
        public static int GetAttributeValueAsInt32(this LiquidElement lqElement, string attrName)
        {
            return (int)lqElement.GetAttributeValue(attrName);
        }
        public static LiquidArray GetAttributeValueAsArray(this LiquidElement lqElement, string attrName)
        {
            return lqElement.GetAttributeValue(attrName) as LiquidArray;
        }
        //-----------------------------------------------------------------------
        public static void WriteJson(this LiquidDoc lqdoc, StringBuilder stBuilder)
        {
            //write to 
            var docElem = lqdoc.DocumentElement;
            if (docElem != null)
            {
                WriteJson(docElem, stBuilder);
            }
        }
        static void WriteJson(object lqElem, StringBuilder stBuilder)
        {
            //recursive
            if (lqElem == null)
            {
                stBuilder.Append("null");
            }
            else if (lqElem is string)
            {
                stBuilder.Append('"');
                stBuilder.Append((string)lqElem);
                stBuilder.Append('"');
            }
            else if (lqElem is double)
            {
                stBuilder.Append(((double)lqElem).ToString());
            }
            else if (lqElem is float)
            {
                stBuilder.Append(((float)lqElem).ToString());
            }
            else if (lqElem is int)
            {
                stBuilder.Append(((int)lqElem).ToString());
            }
            else if (lqElem is Array)
            {
                stBuilder.Append('[');
                //write element into array
                Array a = lqElem as Array;
                int j = a.Length;
                for (int i = 0; i < j; ++i)
                {
                    WriteJson(a.GetValue(i), stBuilder);
                    if (i > 0)
                    {
                        stBuilder.Append(',');
                    }
                }
                stBuilder.Append(']');
            }
            else if (lqElem is LqElement)
            {
                LqElement leqE = (LqElement)lqElem;
                stBuilder.Append('{');
                //check docattr= 
                var nameAttr = leqE.GetAttributeElement("!n");
                if (nameAttr == null)
                {
                    //use specific name
                    stBuilder.Append("\"!n\":\"");
                    stBuilder.Append(leqE.Name);
                    stBuilder.Append('"');
                }
                else
                {
                    //use default elementname
                    stBuilder.Append("\"!n\":\"");
                    stBuilder.Append(leqE.Name);
                    stBuilder.Append('"');
                }

                int attrCount = 1;
                foreach (var attr in leqE.GetAttributeIterForward())
                {
                    if (attr.Name == "!n")
                    {
                        continue;
                    }
                    stBuilder.Append(',');
                    stBuilder.Append('"');
                    stBuilder.Append(attr.Name);
                    stBuilder.Append('"');
                    stBuilder.Append(':');
                    WriteJson(attr.Value, stBuilder);

                    attrCount++;
                }
                //-------------------
                //for children nodes
                int j = leqE.ChildCount;
                //create children nodes
                if (j > 0)
                {
                    stBuilder.Append(',');
                    stBuilder.Append("\"!c\":[");
                    for (int i = 0; i < j; ++i)
                    {
                        WriteJson(leqE.GetChild(i), stBuilder);
                        if (i < j - 1)
                        {
                            stBuilder.Append(',');
                        }
                    }
                    stBuilder.Append(']');
                }
                //-------------------
                stBuilder.Append('}');
            }
            else
            {

            }

        }
        //-----------------------------------------------------------------------
        public static void WriteXml(this LiquidDoc lqdoc, StringBuilder stbuiolder)
        {
            throw new NotSupportedException();
        }
    }
}
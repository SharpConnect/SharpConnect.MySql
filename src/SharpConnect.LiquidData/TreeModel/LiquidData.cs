//MIT 2015, brezza92, EngineKit and contributors

using System.Collections.Generic;
namespace SharpConnect.LiquidData
{
    public interface LiquidElement
    {
        string Name { get; set; }
        LiquidDoc OwnerDocument { get; }
        bool HasOwnerDocument { get; }
        int NameIndex { get; }
        IEnumerable<LiquidAttribute> GetAttributeIterForward();
        void RemoveAttribute(LiquidAttribute attr);
        void AppendChild(LiquidElement element);
        void AppendAttribute(LiquidAttribute attr);
        LiquidAttribute AppendAttribute(string key, object value);

        object GetAttributeValue(string key);
        LiquidAttribute GetAttributeElement(string key);
        int ChildCount { get; }
        object GetChild(int index);
    }
    public interface LiquidAttribute
    {
        string Name { get; set; }
        object Value { get; set; }
        int AttributeLocalNameIndex { get; }
    }
    public interface LiquidArray
    {
        void AddItem(object item);
        IEnumerable<object> GetIterForward();
        void Clear();
        int Count { get; }
        object this[int index] { get; set; }
    }

    public class LiquidDoc
    {
        Dictionary<string, int> stringTable = new Dictionary<string, int>();
        public LiquidElement CreateElement(string elementName)
        {
            return new LqElement(elementName, this);
        }
        public int GetStringIndex(string str)
        {
            int found;
            stringTable.TryGetValue(str, out found);
            return found;
        }
        public LiquidElement DocumentElement
        {
            get;
            set;
        }
    }
}
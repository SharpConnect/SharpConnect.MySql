//MIT 2015, brezza92, EngineKit and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
namespace SharpConnect.LiquidData
{
    /// <summary>
    /// Json-like Parser
    /// </summary>
    public static class LqParser
    {
#if DEBUG
        public static bool dbug_EnableLogParser = false;
        public static int dbug_file_count = 0;
#endif

        public static LiquidElement Parse(string jsontext)
        {
            return Parse(jsontext.ToCharArray());
        }
        static void NotifyError()
        {
        }
        public static LiquidElement Parse(char[] sourceBuffer)
        {
            return Parse(sourceBuffer, true);
        }
        public static LiquidElement Parse(char[] sourceBuffer, bool reformat)
        {
            //#if DEBUG
            //            debugDataFormatParserLog dbugDataFormatParser = null;
            //            if (dbug_EnableLogParser)
            //            {
            //                dbugDataFormatParser = new debugDataFormatParserLog();
            //                dbugDataFormatParser.Begin("json_" + dbug_file_count + " ");
            //                dbug_file_count++;
            //                dbugDataFormatParser.WriteLine(new string(sourceBuffer));

            //            }
            //#endif


            Stack<object> myVElemStack = new Stack<object>();
            Stack<string> myKeyStack = new Stack<string>();
            object currentObj = null;
            StringBuilder myBuffer = new StringBuilder();
            string lastestKey = "";
            int currentState = 0;
            int j = sourceBuffer.Length;
            bool isDoubleNumber = false;
            bool isSuccess = true;
            bool isInKeyPart = false;
            LiquidDoc doc = new LiquidDoc();
            if (sourceBuffer == null)
            {
                return LiquidElementHelper.CreateXmlElementForDynamicObject(doc);
            }



            //WARNING: custom version, about ending with comma
            //we may use implicit comma feature, 
            //in case we start new line but forget a comma,
            //we auto add comma 

            bool implicitComma = false;
            char openStringWithChar = '"';
            int i = 0;
            //int hexdigiCount = 0;
            //char hex00 = '\0';
            //char hex01 = '\0';
            //char hex02 = '\0';
            //char hex03 = '\0';

            for (i = 0; i < j; i++)
            {
                if (!isSuccess)
                {
                    //#if DEBUG
                    //                    if (dbug_EnableLogParser)
                    //                    {
                    //                        dbugDataFormatParser.IndentLevel = myKeyStack.Count;
                    //                        dbugDataFormatParser.WriteLine("fail at pos=" + i + " on " + currentState);
                    //                    }
                    //#endif
                    break;
                }

                //--------------------------
                char c = sourceBuffer[i];
                //#if DEBUG
                //                if (dbug_EnableLogParser)
                //                {
                //                    dbugDataFormatParser.WriteLine(i + " ," + c.ToString() + "," + currentState);
                //                }
                //#endif
                //-------------------------- 

                switch (currentState)
                {
                    case 0:
                        {
                            if (c == '{')
                            {
                                currentObj = doc.CreateElement("!j");
                                currentState = 1;
                                isInKeyPart = true;
                                myBuffer.Length = 0;//clear
                            }
                            else if (c == '[')
                            {
                                currentObj = new LqArray();
                                currentState = 5;
                                isInKeyPart = false;
                                myBuffer.Length = 0;
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                continue;
                            }
                            else
                            {
                                isSuccess = false;
                                NotifyError();
                                break;
                            }
                        }
                        break;
                    case 1:
                        {
                            if (c == '"' || c == '\'')
                            {
                                openStringWithChar = c;
                                currentState = 2;
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                continue;
                            }
                            else if (c == '}')
                            {
                                if (currentObj is LiquidElement)
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        object velem = myVElemStack.Pop();
                                        if (velem is LiquidElement)
                                        {
                                            lastestKey = myKeyStack.Pop();
                                            AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                            currentObj = velem;
                                        }
                                        else if (velem is LiquidArray)
                                        {
                                            AddVElement((LiquidArray)velem, currentObj);
                                            currentObj = velem;
                                            currentState = 7;
                                            isInKeyPart = false;
                                        }
                                    }
                                }
                                else
                                {
                                    NotifyError();
                                    isSuccess = false;
                                }
                            }
                            else if (char.IsLetter(c) || c == '_')
                            {
                                myBuffer.Append(c);
                                currentState = 9;
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                                break;
                            }
                        }
                        break;
                    case 2:
                        {
                            if (c == '\\')
                            {
                                currentState = 3;
                            }
                            else if (c == openStringWithChar)
                            {
                                if (isInKeyPart)
                                {
                                    lastestKey = myBuffer.ToString();
                                    currentState = 4;
                                    myBuffer.Length = 0;//clear
                                }
                                else
                                {
                                    if (currentObj is LiquidArray)
                                    {
                                        object velem = GetVElement(myBuffer, 0);
                                        if (velem != null)
                                        {
                                            AddVElement((LiquidArray)currentObj, velem);
                                            currentState = 7;
                                        }
                                        else
                                        {
                                            NotifyError();
                                            isSuccess = false;
                                        }
                                    }
                                    else
                                    {
                                        object velem = GetVElement(myBuffer, 0);
                                        if (velem != null)
                                        {
                                            AddVElement((LiquidElement)currentObj, lastestKey, velem);
                                            currentState = 7;
                                        }
                                        else
                                        {
                                            NotifyError();
                                            isSuccess = false;
                                        }
                                    }
                                    myBuffer.Length = 0;//clear
                                }
                            }
                            else
                            {
                                myBuffer.Append(c);
                            }
                        }
                        break;
                    case 3:
                        {
                            switch (c)
                            {
                                case '"':
                                    {
                                        myBuffer.Append('\"');
                                    }
                                    break;
                                case '\'':
                                    {
                                        myBuffer.Append('\'');
                                    }
                                    break;
                                case '/':
                                    {
                                        myBuffer.Append('/');
                                    }
                                    break;
                                case '\\':
                                    {
                                        myBuffer.Append('\\');
                                    }
                                    break;
                                case 'b':
                                    {
                                        myBuffer.Append('\b');
                                    }
                                    break;
                                case 'f':
                                    {
                                        myBuffer.Append('\f');
                                    }
                                    break;
                                case 'r':
                                    {
                                        myBuffer.Append('\r');
                                    }
                                    break;
                                case 'n':
                                    {
                                        myBuffer.Append('\n');
                                    }
                                    break;
                                case 't':
                                    {
                                        myBuffer.Append('\t');
                                    }
                                    break;
                                case 'u':
                                    {
                                        //unicode char in hexa digit
                                        uint c_uint = ParseUnicode(sourceBuffer[i + 1], sourceBuffer[i + 2], sourceBuffer[i + 3], sourceBuffer[i + 4]);
                                        myBuffer.Append((char)c_uint);
                                        i += 4;
                                    }
                                    break;
                                default:
                                    {
                                        NotifyError();
                                        isSuccess = false;
                                    }
                                    break;
                            }
                            if (isSuccess)
                            {
                                currentState = 2;
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    case 4:
                        {
                            if (c == ':')
                            {
                                currentState = 5;
                                isInKeyPart = false;
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                continue;
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                                break;
                            }
                        }
                        break;
                    case 5:
                        {
                            if (c == '"' || c == '\'')
                            {
                                openStringWithChar = c;
                                currentState = 2;
                            }
                            else if (char.IsDigit(c) || c == '-')
                            {
                                myBuffer.Append(c);
                                currentState = 8;
                            }
                            else if (c == '{')
                            {
                                myVElemStack.Push(currentObj);
                                if (currentObj is LiquidElement)
                                {
                                    myKeyStack.Push(lastestKey);
                                }

                                currentObj = doc.CreateElement("!j");
                                currentState = 1;
                                isInKeyPart = true;
                            }
                            else if (c == '[')
                            {
                                myVElemStack.Push(currentObj);
                                if (currentObj is LiquidElement)
                                {
                                    myKeyStack.Push(lastestKey);
                                }

                                currentObj = new LqArray();
                                currentState = 5;
                                isInKeyPart = false;
                            }
                            else if (c == ']')
                            {
                                if (currentObj is LiquidArray)
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        object velem = myVElemStack.Pop();
                                        if (velem is LiquidElement)
                                        {
                                            lastestKey = myKeyStack.Pop();
                                            AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                            currentObj = velem;
                                            currentState = 7;
                                        }
                                        else
                                        {
                                            AddVElement((LiquidArray)velem, currentObj);
                                            currentObj = velem;
                                        }
                                    }
                                }
                                else
                                {
                                    NotifyError();
                                    isSuccess = false;
                                }
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                continue;
                            }
                            else if (c == 'n' || c == 't' || c == 'f')
                            {
                                currentState = 6;
                                myBuffer.Append(c);
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                                break;
                            }
                        }
                        break;
                    case 6:
                        {
                            if (char.IsLetter(c))
                            {
                                myBuffer.Append(c);
                            }
                            else if (c == ']' || c == '}' || c == ',')
                            {
                                if (myBuffer.Length > 0)
                                {
                                    if (!EvaluateElement(currentObj, myBuffer, 3, c, lastestKey))
                                    {
                                        NotifyError();
                                        isSuccess = false;
                                        break;
                                    }
                                }

                                if (c == ']')
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        currentObj = myVElemStack.Pop();
                                    }
                                }
                                else if (c == '}')
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        currentObj = myVElemStack.Pop();
                                        lastestKey = myKeyStack.Pop();
                                    }
                                }
                                else
                                {
                                    if (currentObj is LiquidElement)
                                    {
                                        currentState = 1;
                                        isInKeyPart = true;
                                    }
                                    else if (currentObj is LiquidArray)
                                    {
                                        currentState = 5;
                                    }
                                }
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                            }
                        }
                        break;
                    case 7:
                        {
                            if (c == ',')
                            {
                                if (currentObj is LiquidElement)
                                {
                                    currentState = 1;
                                    isInKeyPart = true;
                                }
                                else
                                {
                                    currentState = 5;
                                }
                            }
                            else if (c == ']')
                            {
                                if (myVElemStack.Count > 0)
                                {
                                    object velem = myVElemStack.Pop();
                                    if (velem is LiquidElement)
                                    {
                                        lastestKey = myKeyStack.Pop();
                                        AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                        currentObj = velem;
                                    }
                                    else
                                    {
                                        AddVElement((LiquidArray)velem, currentObj);
                                        currentObj = velem;
                                    }
                                }
                            }
                            else if (c == '}')
                            {
                                if (myVElemStack.Count > 0)
                                {
                                    object velem = myVElemStack.Pop();
                                    if (velem is LiquidElement)
                                    {
                                        lastestKey = myKeyStack.Pop();
                                        AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                        currentObj = velem;
                                    }
                                    else
                                    {
                                        AddVElement((LiquidArray)velem, currentObj);
                                        currentObj = velem;
                                    }
                                }
                            }
                            else if (c == '\r' || c == '\n')
                            {
                                //WARNING: review implivit comma
                                implicitComma = true;
                            }
                            else
                            {
                                //WARNING: review implivit comma
                                if (char.IsLetter(c) || c == '_' || c == '"')
                                {
                                    if (implicitComma)
                                    {
                                        if (currentObj is LiquidElement)
                                        {
                                            currentState = 1;
                                            isInKeyPart = true;
                                        }
                                        else
                                        {
                                            currentState = 5;
                                        }
                                        i--;
                                        implicitComma = false;
                                    }
                                }
                            }
                        }
                        break;
                    case 8:
                        {
                            if (char.IsDigit(c))
                            {
                                myBuffer.Append(c);
                            }
                            else if (c == '.')
                            {
                                if (!isDoubleNumber)
                                {
                                    myBuffer.Append(c);
                                    isDoubleNumber = true;
                                }
                                else
                                {
                                    NotifyError();
                                    isSuccess = false;
                                    break;
                                }
                            }

                            else if (c == ']' || c == '}' || c == ',')
                            {
                                int suggestedType = 1;
                                if (isDoubleNumber)
                                {
                                    suggestedType = 2;
                                    isDoubleNumber = false;
                                }
                                if (myBuffer.Length > 0)
                                {
                                    if (!EvaluateElement(currentObj, myBuffer, suggestedType, c, lastestKey))
                                    {
                                        NotifyError();
                                        isSuccess = false;
                                        break;
                                    }
                                }
                                if (c == ']')
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        object velem = myVElemStack.Pop();
                                        if (velem is LiquidElement)
                                        {
                                            lastestKey = myKeyStack.Pop();
                                            AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                            currentObj = velem;
                                        }
                                        else
                                        {
                                            AddVElement((LiquidArray)velem, currentObj);
                                            currentObj = velem;
                                        }
                                    }
                                }
                                else if (c == '}')
                                {
                                    if (myVElemStack.Count > 0)
                                    {
                                        object velem = myVElemStack.Pop();
                                        if (velem is LiquidElement)
                                        {
                                            lastestKey = myKeyStack.Pop();
                                            AddVElement((LiquidElement)velem, lastestKey, currentObj);
                                            currentObj = velem;
                                        }
                                        else
                                        {
                                            AddVElement((LiquidArray)velem, currentObj);
                                            currentObj = velem;
                                        }
                                    }
                                }
                                else
                                {
                                    if (currentObj is LiquidElement)
                                    {
                                        currentState = 1;
                                        isInKeyPart = true;
                                    }
                                    else if (currentObj is LiquidArray)
                                    {
                                        currentState = 5;
                                    }
                                }
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                            }
                        }
                        break;
                    case 9:
                        {
                            if (char.IsLetter(c) || c == '_')
                            {
                                myBuffer.Append(c);
                            }
                            else if (c == ':')
                            {
                                if (isInKeyPart)
                                {
                                    lastestKey = myBuffer.ToString();
                                    myBuffer.Length = 0;//clear
                                    currentState = 5;
                                    isInKeyPart = false;
                                }
                                else
                                {
                                    NotifyError();
                                    isSuccess = false;
                                }
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                currentState = 4;
                            }
                            else
                            {
                                NotifyError();
                                isSuccess = false;
                            }
                        }
                        break;
                }
            }

            //=======================================
#if DEBUG
            //if (dbug_EnableLogParser)
            //{
            //    dbugDataFormatParser.End();
            //}
#endif
            //======================================= 

            if (currentObj is LiquidElement && isSuccess)
            {
                //WARNNIG: reformat is our extension
                if (reformat)
                {
                    ReFormatLqElement((LiquidElement)currentObj);
                }
                return (LiquidElement)currentObj;
            }
            else
            {
                return null;
            }
        }
        private static uint ParseSingleChar(char c1, uint multipliyer)
        {
            uint p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (uint)(c1 - '0') * multipliyer;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (uint)((c1 - 'A') + 10) * multipliyer;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (uint)((c1 - 'a') + 10) * multipliyer;
            return p1;
        }

        private static uint ParseUnicode(char c1, char c2, char c3, char c4)
        {
            uint p1 = ParseSingleChar(c1, 0x1000);
            uint p2 = ParseSingleChar(c2, 0x100);
            uint p3 = ParseSingleChar(c3, 0x10);
            uint p4 = ParseSingleChar(c4, 1);
            return p1 + p2 + p3 + p4;
        }
        /// <summary>
        /// convert json to lq element
        /// </summary>
        /// <param name="element"></param>
        static void ReFormatLqElement(LiquidElement element)
        {
            LiquidAttribute childNodeAttr = null;
            LiquidAttribute nodeNameAttr = null;
            if (!element.HasOwnerDocument)
            {
                return;
            }
            LiquidDoc ownerdoc = element.OwnerDocument;
            int found_N = element.OwnerDocument.GetStringIndex("!n");
            int found_C = element.OwnerDocument.GetStringIndex("!c");
            foreach (LiquidAttribute att in element.GetAttributeIterForward())
            {
                if (found_N != 0 && att.AttributeLocalNameIndex == found_N) //!n
                {
                    element.Name = att.Value.ToString();
                    nodeNameAttr = att;
                }
                else if (found_C != 0 && att.AttributeLocalNameIndex == found_C)
                {
                    childNodeAttr = att;
                }
            }
            //--------------------------------------
            if (nodeNameAttr != null)
            {
                element.RemoveAttribute(nodeNameAttr);
            }
            //--------------------------------------

            if (childNodeAttr != null)
            {
                if (childNodeAttr.Value is LiquidArray)
                {
                    LiquidArray children = (LiquidArray)childNodeAttr.Value;
                    foreach (object child in children.GetIterForward())
                    {
                        if (child is LiquidElement)
                        {
                            ReFormatLqElement((LiquidElement)child);
                            element.AppendChild((LiquidElement)child);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    children.Clear();
                    element.RemoveAttribute(childNodeAttr);
                }
            }
        }

        static bool EvaluateElement(object currentObj, StringBuilder myBuffer, int suggestedType, char terminateChar, string lastestKey)
        {
            if (terminateChar == ']')
            {
                object elem = GetVElement(myBuffer, suggestedType);
                if (currentObj is LiquidArray)
                {
                    AddVElement((LiquidArray)currentObj, elem);
                    myBuffer.Length = 0;
                    return true;
                }
            }
            else if (terminateChar == '}')
            {
                object elem = GetVElement(myBuffer, suggestedType);
                if (currentObj is LiquidElement)
                {
                    AddVElement((LiquidElement)currentObj, lastestKey, elem);
                    myBuffer.Length = 0;
                    return true;
                }
            }
            else if (terminateChar == ',')
            {
                object elem = GetVElement(myBuffer, suggestedType);
                if (elem != null)
                {
                    if (currentObj is LiquidElement)
                    {
                        AddVElement((LiquidElement)currentObj, lastestKey, elem);
                        myBuffer.Length = 0;
                        return true;
                    }
                    else if (currentObj is LiquidArray)
                    {
                        AddVElement((LiquidArray)currentObj, elem);
                        myBuffer.Length = 0;
                        return true;
                    }
                }
            }

            return false;
        }
        static object GetVElement(StringBuilder myBuffer, int suggestedType)
        {
            switch (suggestedType)
            {
                case 0:
                    {
                        return myBuffer.ToString();
                    }
                case 1://int
                    {
                        int intNumber = 0;
                        try
                        {
                            return Convert.ToInt32(myBuffer.ToString());
                        }
                        catch
                        {
                            return null;
                        }
                    }
                case 2://double
                    {
                        double doubleNumber = 0;
                        try
                        {
                            return Convert.ToDouble(myBuffer.ToString());
                        }
                        catch
                        {
                            return null;
                        }
                    }
                case 3:
                    {
                        string myvalue = myBuffer.ToString();
                        if (myvalue == "true")
                        {
                            return true;
                        }
                        else if (myvalue == "false")
                        {
                            return false;
                        }
                        else if (myvalue == "null")
                        {
                            return null;
                        }
                    }
                    break;
            }
            return null;
        }
        static void AddVElement(LiquidArray dArray, object velemt)
        {
            dArray.AddItem(velemt);
        }
        static void AddVElement(LiquidElement dObj, string key, object velemt)
        {
            dObj.AppendAttribute(key, velemt);
        }
    }
}
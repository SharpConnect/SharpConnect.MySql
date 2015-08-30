//LICENSE: MIT 
//Copyright(c) 2015 brezza27, EngineKit and contributors 

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace MySqlPacket
{
    enum ParseState
    {
        FIND_MARKER,
        GET_KEY
    }

    class SqlStringTemplate
    {
        List<string> _sqlSections = new List<string>(); //all sections
        List<string> _keys = new List<string>(); //all keys
        List<string> _valuesKeys = new List<string>(); //only value keys        

        private SqlStringTemplate()
        {

        }

        public List<string> GetValueKeys()
        {
            return _valuesKeys;
        }

        public static SqlStringTemplate ParseSql(string rawSql, CommandParams sampleCmdParams)
        {

            var sqlStringTemplate = new SqlStringTemplate();

            List<string> sqlSection = sqlStringTemplate._sqlSections;
            List<string> keys = sqlStringTemplate._keys;
            int length = rawSql.Length;

            ParseState state = ParseState.FIND_MARKER;
            char ch;

            StringBuilder strBuilder = new StringBuilder();
            string temp;
            for (int i = 0; i < length; i++)
            {
                ch = rawSql[i];
                switch (state)
                {
                    case ParseState.FIND_MARKER:
                        if (ch == '?')
                        {
                            temp = strBuilder.ToString();
                            sqlSection.Add(temp);
                            strBuilder.Length = 0;
                            state = ParseState.GET_KEY;
                            //continue;
                        }
                        strBuilder.Append(ch);
                        break;
                    case ParseState.GET_KEY:
                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                        {
                            strBuilder.Append(ch);
                        }
                        else
                        {
                            temp = strBuilder.ToString();
                            sqlSection.Add(temp);
                            keys.Add(temp);
                            strBuilder.Length = 0;
                            state = ParseState.FIND_MARKER;

                            strBuilder.Append(ch);
                        }
                        break;
                    default:
                        break;
                }//end swicth
            }//end for
            temp = strBuilder.ToString();
            if (state == ParseState.GET_KEY)
            {
                keys.Add(temp);
            }
            sqlSection.Add(temp);
            //-------------------------------------------------------------------------

             
            sqlStringTemplate.FindValueKeys(sampleCmdParams);
            //-------------------------------------------------------------------------
            return sqlStringTemplate;
        }

        void FindValueKeys(CommandParams sampleCmdParams)
        {
            int count = _keys.Count;
            for (int i = 0; i < count; i++)
            {
                if (sampleCmdParams.IsValueKeys(_keys[i]))
                {
                    _valuesKeys.Add(_keys[i]);
                }
            }
        }
        public string BindValues(CommandParams cmdParams, bool forPrepareStmt)
        {
            //CombindAndReplaceSqlSection
            StringBuilder strBuilder = new StringBuilder();
            int count = _sqlSections.Count;

            for (int i = 0; i < count; i++)
            {
                if (_sqlSections[i][0] == '?')
                {
                    //TODO: reivew here again
                    string temp = cmdParams.GetFieldName(_sqlSections[i]);
                    if (temp != null)
                    {
                        strBuilder.Append(temp);
                    }
                    else if (cmdParams.IsValueKeys(_sqlSections[i]))
                    {
                        strBuilder.Append('?');
                    }
                    else
                    {
                        if (!forPrepareStmt)
                        {

                            throw new Exception("Error : This key not assign.");
                        }
                        else
                        {
                            strBuilder.Append('?');
                        }
                    }
                }
                else
                {
                    strBuilder.Append(_sqlSections[i]);
                }
            }

            return strBuilder.ToString();
        }
    }
}
//LICENSE: MIT 
//Copyright(c) 2015 brezza27, EngineKit and contributors 

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpConnect.MySql.Internal
{
    //---------------------------------
    //value key => prefix with ? or @
    //special key (extension) prefix with ?? only ***

    enum ParseState
    {
        FIND_MARKER,
        COLLECT_MARKER_KEY,
        COLLECT_SP_MARKER_KEY, //(extension) special marker key for bind with table name, fieldname 
        STRING_ESCAPE,
    }

    enum SqlSectionKind
    {
        SqlText,
        ValueKey,
        SpecialKey,
    }

    class SqlSection
    {
        public readonly SqlSectionKind sectionKind;
        public readonly string Text;
        public SqlSection(string text, SqlSectionKind sectionKind)
        {
            this.Text = text;
            this.sectionKind = sectionKind;
        }
#if DEBUG
        public override string ToString()
        {
            return Text;
        }
#endif
    }

    class SqlBoundSection : SqlSection
    {

        public FieldPacket fieldInfo;

        public SqlBoundSection(string text)
            : base(text, SqlSectionKind.ValueKey)
        {

        }


#if DEBUG
        public override string ToString()
        {
            return Text;
        }
#endif
    }

    class SqlStringTemplate
    {
        List<SqlSection> _sqlSections = new List<SqlSection>(); //all sections 
        List<SqlBoundSection> _valuesKeys = new List<SqlBoundSection>(); //only value keys        
        List<SqlSection> _specialKeys = new List<SqlSection>();

        string _userRawSql; //raw sql from user code

        public SqlStringTemplate(string rawSql)
        {
            _userRawSql = rawSql;
            //------------------------------
            //parse
            int length = rawSql.Length;
            ParseState state = ParseState.FIND_MARKER;
            StringBuilder stBuilder = new StringBuilder();

            //TODO: review parser state, escape ' or " or `

            char binderEscapeChar = '\0';
            char escapeChar = '\0';

            for (int i = 0; i < length; i++)
            {
                char ch = rawSql[i];
                switch (state)
                {
                    default:
                        //unknown state must throw exception, so we can see if something changed
                        throw new NotSupportedException();
                    case ParseState.FIND_MARKER:

                        if (ch == '?' || ch == '@')
                        {
                            binderEscapeChar = ch;

                            //found begining point of new marker
                            if (stBuilder.Length > 0)
                            {
                                _sqlSections.Add(new SqlSection(stBuilder.ToString(), SqlSectionKind.SqlText));
                                stBuilder.Length = 0;
                            }
                            state = ParseState.COLLECT_MARKER_KEY;
                        }
                        else if (ch == '\'' || ch == '"' || ch == '`')
                        {
                            escapeChar = ch;
                            state = ParseState.STRING_ESCAPE;
                        }

                        stBuilder.Append(ch);
                        break;
                    case ParseState.COLLECT_MARKER_KEY:

                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_')
                        {
                            stBuilder.Append(ch);
                        }
                        else if (ch == '?')
                        {
                            //this is special marker key
                            stBuilder.Append(ch);
                            if (binderEscapeChar == '?')
                            {
                                state = ParseState.COLLECT_SP_MARKER_KEY;
                            }
                            else
                            {
                                //eg ?@
                                //error
                                throw new NotSupportedException("syntax err!");
                            }
                        }
                        else if (ch == '@')
                        {
                            stBuilder.Append(ch);
                            if (binderEscapeChar == '@')
                            {
                                //@@ 
                                state = ParseState.FIND_MARKER; //goto normal text state
                            }
                            else
                            {
                                //eg @?
                                //eg ?@
                                //error
                                throw new NotSupportedException("syntax err!");
                            }
                        }
                        else
                        {
                            //value binding marking end here

                            if (stBuilder.Length > 0)
                            {
                                var valueSection = new SqlBoundSection(stBuilder.ToString());
                                _sqlSections.Add(valueSection);
                                _valuesKeys.Add(valueSection);

                                stBuilder.Length = 0;
                            }
                            state = ParseState.FIND_MARKER;
                            stBuilder.Append(ch);
                        }
                        break;
                    case ParseState.COLLECT_SP_MARKER_KEY:
                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_')
                        {
                            stBuilder.Append(ch);
                        }
                        else
                        {
                            //special marker end here
                            if (stBuilder.Length > 0)
                            {
                                var specialSection = new SqlSection(stBuilder.ToString(), SqlSectionKind.SpecialKey);
                                _sqlSections.Add(specialSection);
                                _specialKeys.Add(specialSection);
                                stBuilder.Length = 0;
                            }
                            state = ParseState.FIND_MARKER;

                            stBuilder.Append(ch);
                        }
                        break;
                    case ParseState.STRING_ESCAPE:
                        {
                            if (ch == '\'' || ch == '"' || ch == '`')
                            {
                                if (escapeChar == ch)
                                {
                                    escapeChar = '\0';
                                    //go back to find marker state
                                    state = ParseState.FIND_MARKER;
                                }
                            }
                            stBuilder.Append(ch);
                        }
                        break;

                }//end swicth
            }//end for


            if (stBuilder.Length > 0)
            {

                switch (state)
                {
                    default:
                        throw new NotSupportedException();
                    case ParseState.FIND_MARKER:
                        _sqlSections.Add(new SqlSection(stBuilder.ToString(), SqlSectionKind.SqlText));
                        break;
                    case ParseState.COLLECT_MARKER_KEY:
                        var valueSection = new SqlBoundSection(stBuilder.ToString());
                        _sqlSections.Add(valueSection);
                        _valuesKeys.Add(valueSection);
                        break;
                    case ParseState.COLLECT_SP_MARKER_KEY:
                        var specialSection = new SqlSection(stBuilder.ToString(), SqlSectionKind.SpecialKey);
                        _sqlSections.Add(specialSection);
                        _specialKeys.Add(specialSection);
                        break;
                }
            }

        }

        public List<SqlBoundSection> GetValueKeys()
        {
            return _valuesKeys;
        }


        static void FormatAndAppendData(StringBuilder stbuilder, ref MyStructData data)
        {

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //TODO: review here , data range
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            switch (data.type)
            {
                case Types.BLOB:
                case Types.LONG_BLOB:
                case Types.MEDIUM_BLOB:
                case Types.TINY_BLOB:
                    MySqlStringToHexUtils.ConvertByteArrayToHexWithMySqlPrefix(data.myBuffer, stbuilder);
                    break;
                case Types.DATE:
                case Types.NEWDATE:
                    stbuilder.Append('\'');
                    stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd"));
                    stbuilder.Append('\'');
                    break;
                case Types.DATETIME:
                    stbuilder.Append('\'');
                    stbuilder.Append(data.myDateTime.ToString("yyyy-MM-dd hh:mm:ss"));
                    stbuilder.Append('\'');
                    break;
                case Types.TIMESTAMP:
                case Types.TIME:
                    //TODO: review here
                    stbuilder.Append('\'');
                    stbuilder.Append(data.myDateTime.ToString("hh:mm:ss"));
                    stbuilder.Append('\'');
                    break;
                case Types.STRING:
                case Types.VARCHAR:
                case Types.VAR_STRING:

                    stbuilder.Append('\'');
                    //TODO: check /escape string here ****
                    stbuilder.Append(data.myString);
                    stbuilder.Append('\'');

                    break;
                case Types.BIT:
                    stbuilder.Append(Encoding.ASCII.GetString(new byte[] { (byte)data.myInt32 }));
                    break;
                case Types.DOUBLE:
                    stbuilder.Append(data.myDouble.ToString());
                    break;
                case Types.FLOAT:
                    stbuilder.Append(((float)data.myDouble).ToString());
                    break;
                case Types.TINY:
                case Types.SHORT:
                case Types.LONG:
                case Types.INT24:
                case Types.YEAR:
                    stbuilder.Append(data.myInt32.ToString());
                    break;
                case Types.LONGLONG:
                    stbuilder.Append(data.myInt64.ToString());
                    break;
                case Types.DECIMAL:
                    stbuilder.Append(data.myDecimal.ToString());
                    break;
                default:
                    stbuilder.Append(data.myUInt64.ToString());
                    break;

            }
        }
        public string BindValues(CommandParams cmdParams, bool forPrepareStmt)
        {

            StringBuilder strBuilder = new StringBuilder();
            int count = _sqlSections.Count;

            for (int i = 0; i < count; i++)
            {

                var sqlSection = _sqlSections[i];
                switch (sqlSection.sectionKind)
                {
                    default:
                        throw new NotSupportedException();
                    case SqlSectionKind.SqlText:
                        strBuilder.Append(sqlSection.Text);
                        break;
                    case SqlSectionKind.ValueKey:


                        if (forPrepareStmt)
                        {
                            strBuilder.Append('?');
                        }
                        else
                        {
                            //get bind data
                            MyStructData bindedData;
                            if (!cmdParams.TryGetData(sqlSection.Text, out bindedData))
                            {
                                throw new Exception("Error : This key not assign." + sqlSection.Text);
                            }
                            else
                            {
                                //format data 
                                FormatAndAppendData(strBuilder, ref bindedData);
                            }
                        }
                        break;
                    case SqlSectionKind.SpecialKey:
                        string found;
                        if (cmdParams.TryGetSqlPart(sqlSection.Text, out found))
                        {
                            strBuilder.Append(found);
                        }
                        else
                        {
                            throw new Exception("not found " + sqlSection.Text);
                        }
                        break;
                }
            }

            return strBuilder.ToString();
        }
    }
}
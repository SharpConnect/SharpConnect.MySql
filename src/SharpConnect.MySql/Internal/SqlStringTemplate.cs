//LICENSE: MIT 
//Copyright(c) 2015 brezza27, EngineKit and contributors 

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Threading;
using System.Net;
using System.Net.Sockets;


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


    class SqlStringTemplate
    {
        List<SqlSection> _sqlSections = new List<SqlSection>(); //all sections 
        List<SqlSection> _valuesKeys = new List<SqlSection>(); //only value keys        
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
                        stBuilder.Append(ch);
                        break;
                    case ParseState.COLLECT_MARKER_KEY:

                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
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
                                var valueSection = new SqlSection(stBuilder.ToString(), SqlSectionKind.ValueKey);
                                _sqlSections.Add(valueSection);
                                _valuesKeys.Add(valueSection);

                                stBuilder.Length = 0;
                            }
                            state = ParseState.FIND_MARKER;
                            stBuilder.Append(ch);
                        }
                        break;
                    case ParseState.COLLECT_SP_MARKER_KEY:
                        if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
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
                        var valueSection = new SqlSection(stBuilder.ToString(), SqlSectionKind.ValueKey);
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

        public List<SqlSection> GetValueKeys()
        {
            return _valuesKeys;
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

                        //TODO: review checking technique again 
                        if (forPrepareStmt)
                        {
                            strBuilder.Append('?');
                        }
                        else
                        {
                            if (cmdParams.IsValueKeys(sqlSection.Text))
                            {
                                strBuilder.Append('?');
                            }
                            else
                            {
                                throw new Exception("Error : This key not assign." + sqlSection.Text);
                            }
                        }
                        break;
                    case SqlSectionKind.SpecialKey:
                        string found = cmdParams.GetSpecialKeyValue(sqlSection.Text);
                        if (found != null)
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
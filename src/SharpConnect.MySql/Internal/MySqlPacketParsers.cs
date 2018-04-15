//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2018, brezza92, EngineKit and contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpConnect.Internal;

namespace SharpConnect.MySql.Internal
{

    abstract class MySqlPacketParser
    {
        protected const byte ERROR_CODE = 255;
        protected const byte EOF_CODE = 254;
        protected const byte OK_CODE = 0;
        protected const int PACKET_HEADER_LENGTH = 4;
        protected bool _needMoreData;
        protected bool _isProtocol41;
        public abstract void Reset();
        public abstract void Parse(MySqlStreamReader reader);
        public abstract MySqlResult ParseResult { get; }
        public bool NeedMoreData { get { return _needMoreData; } }
    }

    class ResultPacketParser : MySqlPacketParser
    {
        enum ResultPacketState
        {
            Header_Header,//start
            Header_Content,
            Field_Header,
            Field_Content,
            Field_EOF,
            Row_Header,
            Row_Content,


            Row_EOF,
            ShouldEnd,
            Error_Content,
            Ok_Content,

        }

        ResultPacketState _parsingState;
        PacketHeader _currentHeader;

        TableHeader _tableHeader;
        MySqlResult _parseResult;
        List<DataRowPacket> _rows;
        MySqlMultiTableResult _currentMultResultSet;

        bool _supportPartialRelease = true;
        bool _generateResultMode = true;
        bool _forPrepareResult;

        public ResultPacketParser(bool isProtocol41, bool isPrepare = false)
        {

            this._isProtocol41 = isProtocol41;
            this._forPrepareResult = isPrepare; //binary protocol
        }
        public override void Reset()
        {

        }
        public bool ForPrepareResult
        {
            get
            {
                return _forPrepareResult;
            }
        }

        public bool JustFlushMode
        {
            get { return !_generateResultMode; }
            set { _generateResultMode = !value; }
        }

        bool StepParse(MySqlStreamReader reader)
        {
            //reset everytime before actual parse
            _needMoreData = false;
            switch (_parsingState)
            {
                case ResultPacketState.Header_Header:
                    return Parse_Header_Header(reader);
                case ResultPacketState.Header_Content:
                    return Parse_Header_Content(reader);
                //---------------------------------------
                case ResultPacketState.Field_Header:
                    return Parse_Field_Header(reader);
                case ResultPacketState.Field_Content:
                    return Parse_Field_Content(reader);
                case ResultPacketState.Field_EOF:
                    return Parse_Field_EOF(reader);
                //---------------------------------------
                case ResultPacketState.Row_Header:
                    return Parse_Row_Header(reader);
                case ResultPacketState.Row_Content:
                    return Parse_Row_Content(reader);

                case ResultPacketState.Row_EOF:
                    return Parse_Row_EOF(reader);
                //---------------------------------------
                case ResultPacketState.Error_Content:
                    return Parse_Error_Content(reader);
                case ResultPacketState.Ok_Content:
                    return Parse_Ok_Content(reader);
                default:
                    throw new Exception("unknown step");
            }
        }
        bool Parse_Header_Header(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }
            //-------------------------------- 
            _currentHeader = reader.ReadPacketHeader();
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    _parsingState = ResultPacketState.Error_Content;
                    break;
                case EOF_CODE:
                case OK_CODE:
                    _parsingState = ResultPacketState.Ok_Content;
                    break;
                default:
                    //resultset packet 
                    _parsingState = ResultPacketState.Header_Content;
                    break;
            }
            return false;
        }
        bool Parse_Ok_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }
            if (this._currentMultResultSet != null)
            {
                //in multiple result set mode ***
                //see https://dev.mysql.com/doc/internals/en/multi-resultset.html
                //
                var okPacket = new OkPacket(_currentHeader, this._isProtocol41);
                okPacket.ParsePacketContent(reader);
                _parseResult = _currentMultResultSet;
                _parsingState = ResultPacketState.ShouldEnd; //*
                //
                _currentMultResultSet = null;//reset 
            }
            else
            {
                var okPacket = new OkPacket(_currentHeader, this._isProtocol41);
                okPacket.ParsePacketContent(reader);
                _parseResult = new MySqlOkResult(okPacket);
                _parsingState = ResultPacketState.ShouldEnd; //*
            }
            return true;
        }
        /// <summary>
        /// parse header part of the result set
        /// </summary>
        bool Parse_Header_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }
            //can parse
            var resultSetHeaderPacket = new ResultSetHeaderPacket(_currentHeader);
            resultSetHeaderPacket.ParsePacketContent(reader);
            _tableHeader = new TableHeader(this.ForPrepareResult);

            _parsingState = ResultPacketState.Field_Header;
            _rows = new List<DataRowPacket>();
            return false;
        }
        bool Parse_Field_Header(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }

            _currentHeader = reader.ReadPacketHeader();
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    _parsingState = ResultPacketState.Error_Content;
                    break;
                case EOF_CODE:
                case OK_CODE:
                    _parsingState = ResultPacketState.Field_EOF;
                    break;
                default:
                    //next state => field content of this field
                    _parsingState = ResultPacketState.Field_Content;
                    break;
            }
            return false;
        }
        bool Parse_Field_Content(MySqlStreamReader reader)
        {


            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
#if DEBUG
                reader.dbugMonitorData1 = true;
#endif
                return _needMoreData = true;
            }


            var fieldPacket = new FieldPacket(_currentHeader, this._isProtocol41);
            fieldPacket.ParsePacketContent(reader);
            fieldPacket.FieldIndex = _tableHeader.ColumnCount; //set this before  add to field list
            _tableHeader.AddField(fieldPacket);

#if DEBUG
            //TODO:review here
            if (fieldPacket.dbugFailure)
            {
                throw new NotSupportedException();
            }
#endif

            //next state => field header of next field
            _parsingState = ResultPacketState.Field_Header;
            return false;
        }
        bool Parse_Field_EOF(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }
            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            eofPacket.ParsePacketContent(reader);
            _parsingState = ResultPacketState.Row_Header;
            return false;
        }
        bool Parse_Row_Header(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                return _needMoreData = true;
            }

            _currentHeader = reader.ReadPacketHeader();
            if (_currentHeader.ContentLength == Packet.MAX_PACKET_LENGTH)
            {
                //need more than 1 packet for row content 
                _parsingState = ResultPacketState.Row_Content;
                return false;
            }
            else if (_currentHeader.ContentLength > Packet.MAX_PACKET_LENGTH)
            {
                throw new NotSupportedException("???");
            }
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    _parsingState = ResultPacketState.Error_Content;
                    break;
                case EOF_CODE://0x00 or 0xfe the OK packet header
                    _parsingState = ResultPacketState.Row_EOF;
                    break;
                default:
                    _parsingState = ResultPacketState.Row_Content;
                    break;
            }
            return false;
        }

        bool isLargeData = false;
        bool Parse_Row_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }
            if (_currentHeader.ContentLength > Packet.MAX_PACKET_LENGTH)
            {
                throw new NotSupportedException("???");
            }
            else if (_currentHeader.ContentLength >= Packet.MAX_PACKET_LENGTH)
            {
                //can't complete in this round 
                //so store data into temp extra large buffer 
                //and set isLargeData= true
                StoreBuffer(reader, (int)_currentHeader.ContentLength);
                isLargeData = true;
                //we still in the row content state
                _parsingState = ResultPacketState.Row_Header;
                return false;
            }
            //--------------------------------       

            if (_generateResultMode)
            {
                //this is normal mode (opposite to JustFlushOutMode)
                //in this mode we parse packet content 
                //and add it to the output rows 
                //----------------------------------
                //in  this version row buffer len must < int.MaxLength
                if (_currentHeader.ContentLength > int.MaxValue)
                {
                    throw new NotSupportedException("not support this length");
                }
                //------------------------------------  
                if (isLargeData)
                {
                    if (ms == null)
                    {   //it should not be null here
                        throw new NotSupportedException();//?   
                    }
                    ms.Write(reader.ReadBuffer((int)_currentHeader.ContentLength), 0,
                        (int)_currentHeader.ContentLength);
                    _rows.Add(new DataRowPacket(_currentHeader, ms.ToArray()));

#if NET20
                    ms.Close();
#endif
                    ms.Dispose();
                    ms = null;

                    isLargeData = false; //reset
                }
                else
                {
                    _rows.Add(new DataRowPacket(_currentHeader,
                    reader.ReadBuffer((int)_currentHeader.ContentLength)));
                }

            }
            else
            {
                //just flush data*** 
                //not create data row
                if (_currentHeader.ContentLength > int.MaxValue)
                {
                    throw new Exception("not support content length> int.MaxValue");
                }
                reader.SkipForward((int)_currentHeader.ContentLength);
            }
            //-----------------------------------------------------------------------
            //after this row, next state = next row header
            _parsingState = ResultPacketState.Row_Header;
            return false;
        }

        bool Parse_Row_EOF(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }
            //finish all of each row
            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            eofPacket.ParsePacketContent(reader);

            if (((eofPacket.serverStatus & (int)MySqlServerStatus.SERVER_MORE_RESULTS_EXISTS)) != 0)
            {
                var tableResult = new MySqlTableResult(_tableHeader, _rows);
                _rows = null;//reset

                //more than one result table
                //mu
                if (_currentMultResultSet != null)
                {
                    _currentMultResultSet.AddTableResult(tableResult);
                }
                else
                {
                    //first time 
                    _currentMultResultSet = new MySqlMultiTableResult();
                    _currentMultResultSet.AddTableResult(tableResult); ;
                    //not set _parseResult*** because this not finish
                }
                //--------------------
                //see: https://dev.mysql.com/doc/internals/en/multi-resultset.html
                //may has more than 1 result
                _parsingState = ResultPacketState.Header_Header;
                return false;
            }
            else
            {
                //after finish we create a result table 
                //the move rows into the table
                _parseResult = new MySqlTableResult(_tableHeader, _rows);
                //not link to the rows anymore
                _rows = null;
                _currentMultResultSet = null;
                _parsingState = ResultPacketState.ShouldEnd;//***  
                return true;//end
            }
        }
        //---------------------
        MemoryStream ms;
        void StoreBuffer(MySqlStreamReader reader, int length)
        {
            if (ms == null)
            {
                ms = new MemoryStream();
                //buffer data to this 
            }
            ms.Write(reader.ReadBuffer(length), 0, length);

            //reader.SkipForward(length);
            // throw new NotSupportedException();
            ////TODO: review buffer mx here ****
            //byte[] dataTemp = _mysqlStreamReader.ReadBuffer((int)length);
            //int existingLargeDataBufferLen = (largeDataBuffer == null) ?
            //    0 :
            //    largeDataBuffer.Length;
            //if (existingLargeDataBufferLen > 0)
            //{
            //    //merge ...
            //    byte[] newData = new byte[existingLargeDataBufferLen + dataTemp.Length];
            //    Buffer.BlockCopy(largeDataBuffer, 0, newData, 0, largeDataBuffer.Length);
            //    Buffer.BlockCopy(dataTemp, 0, newData, largeDataBuffer.Length, dataTemp.Length);
            //    largeDataBuffer = newData;
            //}
            //else
            //{
            //    largeDataBuffer = dataTemp;
            //}
        }
        bool Parse_Error_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
                return _needMoreData = true;
            }
            var errPacket = new ErrPacket(_currentHeader);
            errPacket.ParsePacketContent(reader);
            //------------------------
            _parseResult = new MySqlErrorResult(errPacket);
            _parsingState = ResultPacketState.ShouldEnd;
            return true;//finished
        }

        public override void Parse(MySqlStreamReader reader)
        {
            //reset final result 
            _parseResult = null;
            while (!StepParse(reader)) ;
            //StepParse() return true if 
            //1. need more data or
            //2. finish
            if (_needMoreData)
            {
                //at any state if need more buffer 
                //then stop parsing and return 
                if (_supportPartialRelease)
                {
                    if (_rows != null && _rows.Count > 0)
                    {
                        _parseResult = new MySqlTableResult(_tableHeader, _rows) { HasFollower = true };
                    }
                    if (_generateResultMode)
                    {
                        //generate new result
                        _rows = new List<DataRowPacket>();
                    }
                }
            }
            else if (_parsingState == ResultPacketState.ShouldEnd)
            {
                //reset
                reader.Reset();
                _parsingState = ResultPacketState.Header_Header;
            }
        }

        public override MySqlResult ParseResult
        {
            get
            {
                return _parseResult;
            }
        }
    }

    class PrepareResponsePacketParser : MySqlPacketParser
    {
        enum PrepareResponseParseState
        {
            OkPrepare_Header,//start ***
            OkPrepare_Content,
            BindingField_Header,
            BindingField_Content,
            BindingField_EOF,
            ColumnField_Header,
            ColumnField_Content,
            ColumnField_EOF,
            ShouldEnd,
            Error_Content
        }

        PrepareResponseParseState _parsingState;
        PacketHeader _currentHeader;
        TableHeader _tableHeader;

        MySqlResult _finalResult;
        OkPrepareStmtPacket _okPrepare;

        public PrepareResponsePacketParser(bool isProtocol41)
        {
            this._isProtocol41 = isProtocol41;
        }
        public override void Parse(MySqlStreamReader reader)
        {
            _finalResult = null;
            while (!StepParse(reader)) ;
            //StepParse() return true if 
            //1. need more data or
            //2. finish
        }
        public override void Reset()
        {

        }
        public override MySqlResult ParseResult
        {
            get
            {
                return _finalResult;
            }
        }

        /// <summary>
        /// return ***true*** if finish or need more data
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        bool StepParse(MySqlStreamReader reader)
        {
            switch (_parsingState)
            {
                case PrepareResponseParseState.OkPrepare_Header:
                    return Parse_PrepareOk_Header(reader);
                case PrepareResponseParseState.OkPrepare_Content:
                    return Parse_PrepareOk_Content(reader);
                //------------------------------------------------
                case PrepareResponseParseState.BindingField_Header:
                    return Parse_BindingField_Header(reader);
                case PrepareResponseParseState.BindingField_Content:
                    return Parse_BindingField_Content(reader);
                case PrepareResponseParseState.BindingField_EOF:
                    return Parse_BindingField_EOF(reader);
                //------------------------------------------------
                case PrepareResponseParseState.ColumnField_Header:
                    return Parse_ColumnField_Header(reader);
                case PrepareResponseParseState.ColumnField_Content:
                    return Parse_ColumnField_Content(reader);
                case PrepareResponseParseState.ColumnField_EOF:
                    return Parse_ColumnField_EOF(reader);
                //------------------------------------------------
                case PrepareResponseParseState.ShouldEnd:
                    reader.Reset();
                    return true;
                case PrepareResponseParseState.Error_Content:
                    return ParseErrorPacket(reader);
                default:
                    throw new Exception("unknown step?");
            }
        }
        bool Parse_PrepareOk_Header(MySqlStreamReader reader)
        {
            //this is first step ***
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                return _needMoreData = true;
            }
            _currentHeader = reader.ReadPacketHeader();
            byte type = reader.PeekByte();
            switch (type)
            {
                case ERROR_CODE:
                    _parsingState = PrepareResponseParseState.Error_Content;
                    break;
                case EOF_CODE:
                case OK_CODE:
                    _parsingState = PrepareResponseParseState.OkPrepare_Content;
                    break;
                default:
                    throw new NotSupportedException("Packet type don't match!!");
            }
            return false;
        }
        bool Parse_PrepareOk_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }
            var okPrepare = new OkPrepareStmtPacket(_currentHeader);
            okPrepare.ParsePacketContent(reader);
            _okPrepare = okPrepare;
            //----------------------------------------------------
            _tableHeader = new TableHeader(true);
            //----------------------------------------------------
            //*** 3 possible way after read prepare ok header***
            if (okPrepare.num_params == 0)
            {
                //if prepare stmt dosn't have binding parameters
                if (okPrepare.num_columns > 0)
                {
                    //has some column
                    _parsingState = PrepareResponseParseState.ColumnField_Header;
                }
                else
                {
                    _finalResult = new MySqlPrepareResponseResult(okPrepare, _tableHeader);
                    _parsingState = PrepareResponseParseState.ShouldEnd;
                    reader.Reset();
                    return true; //finish
                }
            }
            else
            {
                _parsingState = PrepareResponseParseState.BindingField_Header;
            }
            return false;
        }
        //----------------------------------------------------
        bool Parse_BindingField_Header(MySqlStreamReader reader)
        {

            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                return _needMoreData = true; //need more
            }
            _currentHeader = reader.ReadPacketHeader();
            byte type = reader.PeekByte();
            switch (type)
            {
                case ERROR_CODE:
                    _parsingState = PrepareResponseParseState.Error_Content;
                    break;
                case EOF_CODE:
                    _parsingState = PrepareResponseParseState.BindingField_EOF;
                    break;
                default:
                    _parsingState = PrepareResponseParseState.BindingField_Content;
                    break;
            }
            return false;
        }
        bool Parse_BindingField_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }

            var field = new FieldPacket(_currentHeader, _isProtocol41);
            field.ParsePacketContent(reader);
            field.FieldIndex = _tableHeader.ColumnCount; //set this before  add to field list
            _tableHeader.AddField(field);
            //back to binding params field again
            _parsingState = PrepareResponseParseState.BindingField_Header;
            return false;
        }
        bool Parse_BindingField_EOF(MySqlStreamReader reader)
        {

            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }

            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            eofPacket.ParsePacketContent(reader);

            if (_okPrepare.num_columns > 0)
            {
                _parsingState = PrepareResponseParseState.ColumnField_Header;
                return false;
            }
            else
            {
                _finalResult = new MySqlPrepareResponseResult(_okPrepare, _tableHeader);
                _parsingState = PrepareResponseParseState.ShouldEnd;
                reader.Reset();
                return true;
            }

        }
        //----------------------------------------------------
        bool Parse_ColumnField_Header(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                return _needMoreData = true;
            }
            _currentHeader = reader.ReadPacketHeader();
            byte type = reader.PeekByte();//1
            switch (type)
            {
                case ERROR_CODE:
                    _parsingState = PrepareResponseParseState.Error_Content;
                    break;
                case EOF_CODE:
                    //?
                    _parsingState = PrepareResponseParseState.ColumnField_EOF;
                    break;
                default:
                    _parsingState = PrepareResponseParseState.ColumnField_Content;
                    break;
            }
            return false;
        }
        bool Parse_ColumnField_Content(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }
            var field = new FieldPacket(_currentHeader, _isProtocol41);
            field.ParsePacketContent(reader);
            field.FieldIndex = _tableHeader.ColumnCount; //set this before  add to field list
            _tableHeader.AddField(field);
            //back to field header
            _parsingState = PrepareResponseParseState.ColumnField_Header;
            return false;
        }
        bool Parse_ColumnField_EOF(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }

            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            eofPacket.ParsePacketContent(reader);
            //
            _finalResult = new MySqlPrepareResponseResult(_okPrepare, _tableHeader);
            _parsingState = PrepareResponseParseState.ShouldEnd;
            reader.Reset();
            return true;//finish
        }
        //----------------------------------------------------
        bool ParseErrorPacket(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                return _needMoreData = true;
            }
            var errPacket = new ErrPacket(_currentHeader);
            errPacket.ParsePacketContent(reader);
            // 
            _finalResult = new MySqlErrorResult(errPacket);
            _parsingState = PrepareResponseParseState.ShouldEnd;
            reader.Reset();
            return true;
        }

    }

    class MySqlConnectionPacketParser : MySqlPacketParser
    {
        HandshakePacket _handshake;
        MySqlHandshakeResult _finalResult;
        public MySqlConnectionPacketParser()
        {
        }
        public override MySqlResult ParseResult
        {
            get
            {
                return _finalResult;
            }
        }

        public override void Parse(MySqlStreamReader reader)
        {
            _finalResult = null;
            //1.create connection frame  
            //_writer.Reset();        
            PacketHeader header = reader.ReadPacketHeader();
            _handshake = new HandshakePacket(header);
            //check if  
            _handshake.ParsePacketContent(reader);
            _finalResult = new MySqlHandshakeResult(_handshake);
        }
        public override void Reset()
        {

        }
    }



    /// <summary>
    /// mysql parser manager
    /// </summary>
    class MySqlParserMx
    {
        ConnectionConfig userConfig;


        MySqlPacketParser currentPacketParser; //current parser 
        bool _isCompleted;
        bool _isProtocol41;

        //-------------------------
        //shared mysql stream reader 
        MySqlStreamReader _mysqlStreamReader = new MySqlStreamReader(Encoding.UTF8);
        //-------------------------
        //built in sub-parsers
        MySqlConnectionPacketParser connParser;
        PrepareResponsePacketParser prepareResponseParser;
     
        //-------------------------
        public MySqlParserMx(ConnectionConfig userConfig)
        {
            this.userConfig = userConfig;
            connParser = new MySqlConnectionPacketParser(); 
            //tableHeader.TypeCast = this.config.typeCast;
        }
        public void SetProtocol41(bool value)
        {
            this._isProtocol41 = value;
        }
        public void UseConnectionParser()
        {
            //switch from current parser to another
            ParseResult = null;
            currentPacketParser = connParser;
            _mysqlStreamReader.Reset();
        }


        public void UseResultParser(bool forPreparedResult = false)
        {
            //switch from current parser to another
            ParseResult = null;

            currentPacketParser = new ResultPacketParser(_isProtocol41, forPreparedResult);
            _mysqlStreamReader.Reset();
        }
        public void UsePrepareResponseParser()
        {
            //switch from current parser to another
            ParseResult = null;
            prepareResponseParser = new PrepareResponsePacketParser(this._isProtocol41);
            currentPacketParser = prepareResponseParser;
            _mysqlStreamReader.Reset();
        }

        //block ***
        public void UseFlushMode(bool value)
        {
            ResultPacketParser resultPacketParser = currentPacketParser as ResultPacketParser;
            if (resultPacketParser != null)
            {
                resultPacketParser.JustFlushMode = value;
                //1. switch parser mx to flush (data) out mode
                //( just read header no new result created)
                //wait*** until we fetch all server data  
            }
        }

        public MySqlResult ParseResult
        {
            get;
            private set;
        }

        public bool IsComplete
        {
            get { return _isCompleted; }
        }

        /// <summary>
        /// return true if not complete
        /// </summary>
        /// <param name="recvIO"></param>
        /// <returns></returns>
        public bool ParseData(RecvIO recvIO)
        {
            //we need to parse some data here 
            //load incomming data into ms 
            //load data from recv buffer into the ms
            //---------------
            //copy all to stream 
            //may not complete in first round ***  
            _mysqlStreamReader.AppendBuffer(recvIO, recvIO.BytesTransferred);
            currentPacketParser.Parse(_mysqlStreamReader);
            //-----------------------------------------------
            //some large table may not complete in first round
            ParseResult = currentPacketParser.ParseResult;
            return !(_isCompleted = !currentPacketParser.NeedMoreData);
            //--------------------
            //not need to wait here
            //just return *** 
            //--------------------
        }
    }

}
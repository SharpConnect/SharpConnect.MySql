//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
//Copyright(c) 2013 Andrey Sidorov(sidorares @yandex.ru) and contributors
//MIT, 2015-2016, brezza92, EngineKit and contributors

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
            ExpectResultSetHeaderPacket,
            ExpectResultSetHead,
            Expect_FieldHeader,
            Field_Content,
            // Field_EofContent,

            Expect_RowHeader,
            Row_Content,
            //Row_EofContent,
            Should_End
        }

        QueryParsingConfig _config;
        ResultPacketState _parsingState;
        PacketHeader _currentHeader;
        Packet _currentPacket;
        TableHeader _tableHeader;
        MySqlResult _parseResult;
        List<DataRowPacket> _rows;

        bool _supportPartialRelease = true;
        bool _generateResultMode = true;
        public ResultPacketParser(QueryParsingConfig config, bool isProtocol41, bool isPrepare = false)
        {
            this._config = config;
            this._isProtocol41 = isProtocol41;
            this.ForPrepareResult = isPrepare;
        }
        public override void Reset()
        {

        }
        public bool ForPrepareResult
        {
            //changable
            get;
            set;
        }

        public bool JustFlushMode
        {
            get { return !_generateResultMode; }
            set { _generateResultMode = !value; }
        }

        void StepParse(MySqlStreamReader reader)
        {
            //reset everytime before actual parse
            _needMoreData = false;
            switch (_parsingState)
            {
                case ResultPacketState.ExpectResultSetHeaderPacket:
                    {
                        ParseResultsetHeaderPacket(reader);
                    }
                    break;
                case ResultPacketState.ExpectResultSetHead:
                    {
                        ParseResultSetHead(reader);
                    }
                    break;
                case ResultPacketState.Expect_FieldHeader:
                    {
                        ParseFieldHeader(reader);
                    }
                    break;
                case ResultPacketState.Field_Content:
                    {
                        ParseFieldContent(reader);
                    }
                    break;
                case ResultPacketState.Expect_RowHeader:
                    {
                        ParseRowHeader(reader);
                    }
                    break;
                case ResultPacketState.Row_Content:
                    {
                        ParseRowContent(reader);
                    }
                    break;
            }
        }
        void ParseResultsetHeaderPacket(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                _needMoreData = true;
                return;
            }
            //-------------------------------- 
            _currentHeader = reader.ReadPacketHeader();
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        ParseErrorPacket(reader);
                    }
                    break;
                case EOF_CODE:
                case OK_CODE:
                    {
                        ParseOkPacket(reader);
                        this._parsingState = ResultPacketState.Should_End;
                    }
                    break;
                default:
                    {
                        //resultset packet
                        _currentPacket = new ResultSetHeaderPacket(_currentHeader);
                        this._parsingState = ResultPacketState.ExpectResultSetHead;
                    }
                    break;
            }
        }
        /// <summary>
        /// parse header part of the result set
        /// </summary>
        void ParseResultSetHead(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                _needMoreData = true;
                return;
            }
            //can parse
            _currentPacket.ParsePacket(reader);
            _tableHeader = new TableHeader();
            _tableHeader.ParsingConfig = this._config;
            _parsingState = ResultPacketState.Expect_FieldHeader;
            _rows = new List<DataRowPacket>();
        }
        void ParseFieldHeader(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                _needMoreData = true;
                return;
            }

            _currentHeader = reader.ReadPacketHeader();
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        ParseErrorPacket(reader);
                    }
                    break;
                case EOF_CODE:
                case OK_CODE:
                    {
                        //after field                        
                        ParseEOFPacket(reader);
                        //next state =>expected row header
                        this._parsingState = ResultPacketState.Expect_RowHeader;
                    }
                    break;
                default:
                    {
                        FieldPacket fieldPacket = new FieldPacket(_currentHeader, this._isProtocol41);
                        _tableHeader.AddField(fieldPacket);
                        _currentPacket = fieldPacket;
                        //next state => field content of this field
                        this._parsingState = ResultPacketState.Field_Content;
                    }
                    break;
            }
        }
        void ParseFieldContent(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength)) //check if length is enough to parse 
            {
                _needMoreData = true;
                return;
            }

            _currentPacket.ParsePacket(reader);
            //next state => field header of next field
            this._parsingState = ResultPacketState.Expect_FieldHeader;
        }
        void ParseRowHeader(MySqlStreamReader reader)
        {
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                _needMoreData = true;
                return;
            }

            _currentHeader = reader.ReadPacketHeader();
            byte packetType = reader.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        //found error
                        ParseErrorPacket(reader);
                    }
                    break;
                case EOF_CODE://0x00 or 0xfe the OK packet header
                    {
                        //finish all of each row
                        ParseEOFPacket(reader);
                        this._parsingState = ResultPacketState.Should_End;//***

                        //after finish we create a result table 
                        //the move rows into the table
                        _parseResult = new MySqlTableResult(_tableHeader, _rows);
                        //not link to the rows anymore
                        _rows = null;
                    }
                    break;
                default:
                    {
                        if (ForPrepareResult)
                        {
                            //create blank datarow packet and set header
                            _currentPacket = new PreparedDataRowPacket(_currentHeader, _tableHeader);
                        }
                        else
                        {   //create blank datarow packet and set header
                            _currentPacket = new DataRowPacket(_currentHeader, _tableHeader);
                        }
                        this._parsingState = ResultPacketState.Row_Content;
                    }
                    break;
            }
        }
        byte[] largeDataBuffer = null;
        bool isLargeData = false;
        void ParseRowContent(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                _needMoreData = true;
                return;
            }
            if (_currentHeader.ContentLength >= Packet.MAX_PACKET_LENGTH)
            {
                //can't complete in this round 
                //so store data into temp extra large buffer 
                //and set isLargeData= true
                StoreBuffer(reader, (int)_currentHeader.ContentLength);
                isLargeData = true;
                //we still in the row content state
                //parsingState = ResultPacketState.Expect_RowHeader; //2016-07-13
                return;
            }
            else
            {
                if (isLargeData)
                {
                    throw new NotSupportedException();
                    //StoreBuffer((int)header.ContentLength);
                    //int remain = (int)(_mysqlStreamReader.CurrentInputLength - _mysqlStreamReader.ReadPosition);
                    //StoreBuffer(remain);
                    ////move large data into a parser
                    //_mysqlStreamReader.LoadNewBuffer(largeDataBuffer, largeDataBuffer.Length);
                    ////reset value***
                    //largeDataBuffer = null;
                    //isLargeData = false;
                }
            }
#if DEBUG
            //check, in debug mode---
            if (ForPrepareResult && !(_currentPacket is PreparedDataRowPacket)) { throw new NotSupportedException(); }
            //-----------------------
#endif
            _currentPacket.ParsePacket(reader);
            if (_generateResultMode)
            {
                //this is normal mode (opposite to JustFlushOutMode)
                //in this mode we parse packet content 
                //and add it to the output rows 
                _rows.Add((DataRowPacket)_currentPacket);
            }
            //-----------------------------------------------------------------------
            //after this row, next state = next row header
            _parsingState = ResultPacketState.Expect_RowHeader;
        }
        void StoreBuffer(MySqlStreamReader reader, int length)
        {
            throw new NotSupportedException();
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
        void ParseErrorPacket(MySqlStreamReader reader)
        {
            var errPacket = new ErrPacket(_currentHeader);
            _currentPacket = errPacket;
            errPacket.ParsePacket(reader);
            //------------------------
            _parseResult = new MySqlErrorResult(errPacket);
        }
        void ParseOkPacket(MySqlStreamReader reader)
        {
            var okPacket = new OkPacket(_currentHeader, this._isProtocol41);
            _currentPacket = okPacket;
            okPacket.ParsePacket(reader);
            _parseResult = new MySqlOkResult(okPacket);
        }
        void ParseEOFPacket(MySqlStreamReader reader)
        {
            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            _currentPacket = eofPacket;
            eofPacket.ParsePacket(reader);
        }
        public override void Parse(MySqlStreamReader reader)
        {
            //reset final result 

            _parseResult = null;
            for (;;)
            {

                StepParse(reader);
                if (_needMoreData)
                {
                    //at any state if need more buffer 
                    //then stop parsing and return 
                    if (_supportPartialRelease)
                    {
                        _parseResult = new MySqlTableResult(_tableHeader, _rows) { IsPartialTable = true };
                        if (_generateResultMode)
                        {
                            _rows = new List<DataRowPacket>();
                        }
                    }

                    return;
                }

                if (_parsingState == ResultPacketState.Should_End)
                {
                    //reset
                    reader.Reset();
                    this._parsingState = ResultPacketState.ExpectResultSetHeaderPacket;
                    return;
                }
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
            ExpectedOkPreparePacket,
            OkPrepare_Content,
            Expect_ParamsFieldHeader,
            ParamsField_Content,
            Params_EOF,
            Expect_ColumnsFieldHeader,
            ColumnsField_Content,
            ColumnsEOF,
            Should_End,
            Error_Content
        }

        PrepareResponseParseState _parsingState;
        PacketHeader _currentHeader;
        TableHeader _tableHeader;
        Packet _currentPacket;
        MySqlResult _finalResult;
        OkPrepareStmtPacket _okPrepare;

        public PrepareResponsePacketParser(bool isProtocol41)
        {
            this._isProtocol41 = isProtocol41;
            _tableHeader = null;
        }


        public override void Parse(MySqlStreamReader reader)
        {
            _finalResult = null;
            for (;;)
            {
                InternalParse(reader);
                if (_needMoreData)
                {
                    return;
                }
                else if (_parsingState == PrepareResponseParseState.Should_End)
                {
                    //reset
                    reader.Reset();
                    return;
                }
            }
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

        void InternalParse(MySqlStreamReader reader)
        {
            switch (_parsingState)
            {
                case PrepareResponseParseState.ExpectedOkPreparePacket:
                    ParseOkPrepareHeader(reader);
                    break;
                case PrepareResponseParseState.OkPrepare_Content:
                    {
                        ParseOkPrepareContent(reader);
                        _parsingState = PrepareResponseParseState.Expect_ParamsFieldHeader;
                        _tableHeader = new TableHeader();
                        break;
                    }
                case PrepareResponseParseState.Expect_ParamsFieldHeader:

                    if (_okPrepare.num_params > 0)
                    {
                        _currentHeader = PacketHeader.Empty;
                        if (!reader.Ensure(PACKET_HEADER_LENGTH))
                        {
                            _needMoreData = true;
                            return;
                        }
                        _currentHeader = reader.ReadPacketHeader();
                        if (!reader.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreData = true;
                            return;
                        }
                        ParseFieldHeader(reader);

                        _parsingState = (_currentPacket is EofPacket) ?
                            PrepareResponseParseState.Params_EOF :
                            PrepareResponseParseState.ParamsField_Content;
                    }
                    else
                    {
                        _parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                    }
                    break;

                case PrepareResponseParseState.ParamsField_Content:
                    {
                        if (!reader.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreData = true;
                            return;
                        }
                        ParseFieldPacket(reader);
                        _parsingState = PrepareResponseParseState.Expect_ParamsFieldHeader;
                        break;
                    }
                case PrepareResponseParseState.Params_EOF:
                    {
                        if (!reader.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreData = true;
                            return;
                        }
                        ParseEOFPacket(reader);
                        if (_currentPacket != null)
                        {
                            _finalResult = new MySqlPrepareResponseResult(_okPrepare, _tableHeader);
                            _tableHeader = new TableHeader();
                            _parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                        }
                        break;
                    }
                case PrepareResponseParseState.Expect_ColumnsFieldHeader:
                    {
                        if (_okPrepare.num_columns > 0)
                        {
                            _currentHeader = PacketHeader.Empty;
                            if (!reader.Ensure(PACKET_HEADER_LENGTH))
                            {
                                _needMoreData = true;
                                return;
                            }

                            _currentHeader = reader.ReadPacketHeader();
                            if (!reader.Ensure(_currentHeader.ContentLength))
                            {
                                _needMoreData = true;
                                return;
                            }
                            ParseFieldHeader(reader);

                            _parsingState = (_currentPacket is EofPacket) ?
                                PrepareResponseParseState.ColumnsEOF :
                                PrepareResponseParseState.ColumnsField_Content;
                        }
                        else
                        {
                            _parsingState = PrepareResponseParseState.Should_End;
                        }
                        break;
                    }
                case PrepareResponseParseState.ColumnsField_Content:
                    {
                        if (!reader.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreData = true;
                            return;
                        }
                        ParseFieldPacket(reader);
                        _parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                        break;
                    }
                case PrepareResponseParseState.ColumnsEOF:
                    if (!reader.Ensure(_currentHeader.ContentLength))
                    {
                        _needMoreData = true;
                        return;
                    }
                    ParseEOFPacket(reader);
                    if (_currentPacket != null)
                    {
                        _finalResult = new MySqlPrepareResponseResult(_okPrepare, _tableHeader);
                        _parsingState = PrepareResponseParseState.Should_End;
                    }
                    break;

                case PrepareResponseParseState.Should_End:
                    break;
                case PrepareResponseParseState.Error_Content:
                    ParseErrorPacket(reader);
                    break;
                default:
                    _parsingState = PrepareResponseParseState.Should_End;
                    break;
            }
        }
        void ParseOkPrepareHeader(MySqlStreamReader reader)
        {
            _currentHeader = PacketHeader.Empty;
            if (!reader.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                _needMoreData = true;
                return;
            }
            _currentHeader = reader.ReadPacketHeader();
            byte type = reader.PeekByte();
            switch (type)
            {
                case ERROR_CODE:
                    ParseErrorPacket(reader);
                    break;
                case EOF_CODE:
                case OK_CODE:
                    _parsingState = PrepareResponseParseState.OkPrepare_Content;
                    break;
                default:
                    _parsingState = PrepareResponseParseState.Should_End;
                    throw new NotSupportedException("Packet type don't match!!");

            }
        }
        void ParseOkPrepareContent(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                _needMoreData = true;
                return;
            }
            var okPrepare = new OkPrepareStmtPacket(_currentHeader);
            okPrepare.ParsePacket(reader);
            this._okPrepare = okPrepare;
        }

        void ParseErrorPacket(MySqlStreamReader reader)
        {
            if (!reader.Ensure(_currentHeader.ContentLength))
            {
                _needMoreData = true;
                _parsingState = PrepareResponseParseState.Error_Content;
                return;
            }
            var errPacket = new ErrPacket(_currentHeader);
            _currentPacket = errPacket;
            errPacket.ParsePacket(reader);
            //------------------------
            this._finalResult = new MySqlErrorResult(errPacket);
            _parsingState = PrepareResponseParseState.Should_End;
        }

        void ParseEOFPacket(MySqlStreamReader reader)
        {
            _currentPacket = null;
            var eofPacket = new EofPacket(_currentHeader, this._isProtocol41);
            _currentPacket = eofPacket;
            eofPacket.ParsePacket(reader);
        }

        void ParseFieldHeader(MySqlStreamReader reader)
        {
            _currentPacket = null;
            if (reader.PeekByte() == EOF_CODE)
            {
                _currentPacket = new EofPacket(_currentHeader, _isProtocol41);
            }
        }
        void ParseFieldPacket(MySqlStreamReader reader)
        {
            FieldPacket field = new FieldPacket(_currentHeader, _isProtocol41);
            field.ParsePacket(reader);
            if (_tableHeader != null)
            {
                _tableHeader.AddField(field);
            }
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
            _handshake.Header = header;
            _handshake.ParsePacket(reader);
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
        QueryParsingConfig parsingConfig;

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
        ResultPacketParser resultPacketParser;
        //-------------------------
        public MySqlParserMx(ConnectionConfig userConfig)
        {
            this.userConfig = userConfig;
            connParser = new MySqlConnectionPacketParser();
            parsingConfig = new QueryParsingConfig()
            {
                TimeZone = userConfig.timezone,
                UseLocalTimeZone = userConfig.timezone.Equals("local"),
                BigNumberStrings = userConfig.bigNumberStrings,
                DateStrings = userConfig.dateStrings,
                SupportBigNumbers = userConfig.supportBigNumbers,
                typeCast = userConfig.typeCast
            };
            //tableHeader.TypeCast = this.config.typeCast;
        }
        public void SetProtocol41(bool value)
        {
            this._isProtocol41 = value;
            if (resultPacketParser == null)
            {
                resultPacketParser = new ResultPacketParser(parsingConfig, value);
            }
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
            //--------------------------------
            //resultPacketParser.ForPrepareResult = forPreparedResult;
            //currentPacketParser = resultPacketParser;
            currentPacketParser = new ResultPacketParser(parsingConfig, _isProtocol41, forPreparedResult);
            _mysqlStreamReader.Reset();
            resultPacketParser.JustFlushMode = false;
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
        public void UseFlushMode()
        {
            ResultPacketParser resultPacketParser = currentPacketParser as ResultPacketParser;
            if (resultPacketParser != null)
            {
                resultPacketParser.JustFlushMode = true;
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
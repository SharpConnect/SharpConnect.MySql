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
using System.IO;
using SharpConnect.Internal;

namespace SharpConnect.MySql.Internal
{

    abstract class MySqlPacketParser
    {
        public abstract void Parse(byte[] buffer, int count);
        public abstract MySqlResult ResultPacket { get; }
        public abstract bool NeedMoreBuffer { get; }
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

        ResultPacketState parsingState;
        /// <summary>
        /// low level mysql packet parser
        /// </summary>
        PacketParser _parser = new PacketParser(Encoding.UTF8);
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 254;
        const byte OK_CODE = 0;
        PacketHeader header;
        Packet currentPacket;
        TableHeader tableHeader;
        ConnectionConfig config;
        bool isProtocol41;
        bool isPrepare;
        bool needMoreBuffer = false;
        MySqlResult _finalResult;
        List<RowDataPacket> rows = new List<RowDataPacket>();
        List<RowPreparedDataPacket> rowsPrepare = new List<RowPreparedDataPacket>();

        const int PACKET_HEADER_LENGTH = 4;
        public ResultPacketParser(ConnectionConfig config, bool isProtocol41, bool isPrepare = false)
        {
            this.config = config;
            this.isProtocol41 = isProtocol41;
            this.isPrepare = isPrepare;
        }

        void Parse()
        {
            needMoreBuffer = false;
            switch (parsingState)
            {
                case ResultPacketState.ExpectResultSetHeaderPacket:
                    {
                        ParseResultsetHeaderPacket();
                    }
                    break;
                case ResultPacketState.ExpectResultSetHead:
                    {
                        ParseResultSetHead();
                    }
                    break;
                case ResultPacketState.Expect_FieldHeader:
                    {
                        ParseFieldHeader();
                    }
                    break;
                case ResultPacketState.Field_Content:
                    {
                        ParseFieldContent();
                    }
                    break;
                case ResultPacketState.Expect_RowHeader:
                    {
                        ParseRowHeader();
                    }
                    break;
                case ResultPacketState.Row_Content:
                    {
                        ParseRowContent();
                    }
                    break;
            }
        }
        void ParseResultsetHeaderPacket()
        {
            if (!_parser.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                needMoreBuffer = true;
                return;
            }
            //-------------------------------- 
            header = _parser.ParsePacketHeader();
            byte packetType = _parser.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        ParseErrorPacket();
                    }
                    break;
                case EOF_CODE:
                case OK_CODE:
                    {
                        ParseOkPacket();
                        this.parsingState = ResultPacketState.Should_End;
                        //ResultAssign(_finalResult);
                    }
                    break;
                default:
                    {
                        //resultset packet
                        (currentPacket = new ResultSetHeaderPacket()).Header = header;
                        this.parsingState = ResultPacketState.ExpectResultSetHead;
                    }
                    break;
            }
        }
        /// <summary>
        /// parse header part of the result set
        /// </summary>
        void ParseResultSetHead()
        {
            if (!_parser.Ensure(header.ContentLength))
            {
                needMoreBuffer = true;
                return;
            }
            //can parse
            currentPacket.ParsePacket(_parser);
            tableHeader = new TableHeader();
            tableHeader.ConnConfig = this.config;
            tableHeader.TypeCast = this.config.typeCast;
            this.parsingState = ResultPacketState.Expect_FieldHeader;
            rows = new List<RowDataPacket>();
        }
        void ParseFieldHeader()
        {
            if (!_parser.Ensure(PACKET_HEADER_LENGTH + 1)) //check if length is enough to parse 
            {
                needMoreBuffer = true;
                return;
            }

            header = _parser.ParsePacketHeader();
            byte packetType = _parser.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        ParseErrorPacket();
                    }
                    break;
                case EOF_CODE:
                case OK_CODE:
                    {
                        //after field
                        //expected field eof  
                        ParseEOFPacket();
                        this.parsingState = ResultPacketState.Expect_RowHeader;
                    }
                    break;
                default:
                    {
                        FieldPacket fieldPacket = new FieldPacket(this.isProtocol41);
                        fieldPacket.Header = header;
                        tableHeader.AddField(fieldPacket);
                        currentPacket = fieldPacket;
                        //next state => field content of this field
                        this.parsingState = ResultPacketState.Field_Content;
                    }
                    break;
            }
        }
        void ParseFieldContent()
        {
            if (!_parser.Ensure(header.ContentLength)) //check if length is enough to parse 
            {
                needMoreBuffer = true;
                return;
            }

            currentPacket.ParsePacket(_parser);
            //next state => field header of next field
            this.parsingState = ResultPacketState.Expect_FieldHeader;
        }
        void ParseRowHeader()
        {
            if (!_parser.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                needMoreBuffer = true;
                return;
            }

            header = _parser.ParsePacketHeader();
            byte packetType = _parser.PeekByte();
            switch (packetType)
            {
                case ERROR_CODE:
                    {
                        //found error
                        ParseErrorPacket();
                    }
                    break;
                case EOF_CODE://0x00 or 0xfe the OK packet header
                    {
                        //finish all of each row
                        ParseEOFPacket();
                        this.parsingState = ResultPacketState.Should_End;//***

                        //after finish we create a result table 
                        //the move rows into the table

                        if (isPrepare)
                        {
                            _finalResult = new MySqlPrepareTableResult(tableHeader, rowsPrepare);
                        }
                        else
                        {
                            _finalResult = new MySqlTableResult(tableHeader, rows);
                        }

                        //not link to the rows anymore
                        rows = null;
                    }
                    break;
                default:
                    {
                        if (isPrepare)
                        {
                            (currentPacket = new RowPreparedDataPacket(tableHeader)).Header = header;
                            //rowsPrepare.Add(rowPacket);
                            //TODO: review here, 
                        }
                        else
                        {
                            (currentPacket = new RowDataPacket(tableHeader)).Header = header;
                            //rows.Add(rowPacket);
                            //TODO: review here, 
                        }
                        this.parsingState = ResultPacketState.Row_Content;
                    }
                    break;
            }
        }
        byte[] largeDataBuffer = null;
        bool isLargeData = false;
        void ParseRowContent()
        {
            if (!_parser.Ensure(header.ContentLength))
            {
                needMoreBuffer = true;
                return;
            }
            if (header.ContentLength >= Packet.MAX_PACKET_LENGTH)
            {
                //can't complete in this round 
                //so store data into temp extra large buffer 
                //and set isLargeData= true
                StoreBuffer((int)header.ContentLength);
                isLargeData = true;
                //we still in the row content state
                //parsingState = ResultPacketState.Expect_RowHeader; //2016-07-13
                return;
            }
            else
            {
                if (isLargeData)
                {
                    StoreBuffer((int)header.ContentLength);
                    int remain = (int)(_parser.CurrentInputLength - _parser.ReadPosition);
                    StoreBuffer(remain);
                    //move large data into a parser
                    _parser.LoadNewBuffer(largeDataBuffer, largeDataBuffer.Length);
                    //reset value***
                    largeDataBuffer = null;
                    isLargeData = false;
                }
            }
            currentPacket.ParsePacket(_parser);
            if (isPrepare)
            {
                //just collect data into row collection
                rowsPrepare.Add((RowPreparedDataPacket)currentPacket);
            }
            else
            {
                rows.Add((RowDataPacket)currentPacket);
            }
            //-----------------------------------------------------------------------
            //after this row, next state = next row header
            this.parsingState = ResultPacketState.Expect_RowHeader;
        }
        void StoreBuffer(int length)
        {
            //TODO: review buffer mx here ****
            byte[] dataTemp = _parser.ParseBuffer((int)length);
            int existingLargeDataBufferLen = (largeDataBuffer == null) ?
                0 :
                largeDataBuffer.Length;
            if (existingLargeDataBufferLen > 0)
            {
                //merge ...
                byte[] newData = new byte[existingLargeDataBufferLen + dataTemp.Length];
                Buffer.BlockCopy(largeDataBuffer, 0, newData, 0, largeDataBuffer.Length);
                Buffer.BlockCopy(dataTemp, 0, newData, largeDataBuffer.Length, dataTemp.Length);
                largeDataBuffer = newData;
            }
            else
            {
                largeDataBuffer = dataTemp;
            }
        }
        void ParseErrorPacket()
        {
            var errPacket = new ErrPacket();
            errPacket.Header = header;
            uint packetLen = errPacket.GetPacketLength();
            currentPacket = errPacket;
            errPacket.ParsePacket(_parser);
            //------------------------
            this._finalResult = new MySqlError(errPacket);
        }
        void ParseOkPacket()
        {
            var okPacket = new OkPacket(this.isProtocol41);
            okPacket.Header = header;
            uint packetLen = okPacket.GetPacketLength();
            currentPacket = okPacket;
            okPacket.ParsePacket(_parser);
            this._finalResult = new MySqlOk(okPacket);
        }
        void ParseEOFPacket()
        {
            EofPacket eofPacket = new EofPacket(this.isProtocol41);
            eofPacket.Header = header;
            currentPacket = eofPacket;
            eofPacket.ParsePacket(_parser);
        }

        public override void Parse(byte[] buffer, int count)
        {
            //reset final result 
            _finalResult = null;
            _parser.AppendBuffer(buffer, count);
            for (;;)
            {
                //loop
                Parse();
                if (needMoreBuffer)
                {
                    //at any state if need more buffer 
                    //then stop parsing and return
                    return;
                }

                if (parsingState == ResultPacketState.Should_End)
                {
                    //reset
                    this._parser.Reset();
                    this.parsingState = ResultPacketState.ExpectResultSetHeaderPacket;
                    return;
                }
            }
        }

        public override MySqlResult ResultPacket
        {
            get
            {
                return _finalResult;
            }
        }

        public override bool NeedMoreBuffer
        {
            get
            {
                return needMoreBuffer;
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
        bool _needMoreBuffer;
        bool _isProtocol41;
        const int PACKET_HEADER_LENGTH = 4;
        const byte ERROR_CODE = 255;
        const byte EOF_CODE = 254;
        const byte OK_CODE = 0;
        MySqlResult _finalResult;
        PacketHeader _currentHeader;
        Packet _currentPacket;
        OkPrepareStmtPacket _okPrepare;
        PacketParser _parser = new PacketParser(Encoding.UTF8);
        PrepareResponseParseState parsingState;
        TableHeader _tableHeader;
        public override bool NeedMoreBuffer
        {
            get
            {
                return _needMoreBuffer;
            }
        }

        public override MySqlResult ResultPacket
        {
            get
            {
                return _finalResult;
            }
        }

        public PrepareResponsePacketParser(bool isProtocol41)
        {
            this._isProtocol41 = isProtocol41;
            _tableHeader = null;
        }

        public override void Parse(byte[] buffer, int count)
        {
            //_finalResult = null;
            _parser.AppendBuffer(buffer, count);
            for (;;)
            {
                Parse();
                if (_needMoreBuffer)
                {
                    return;
                }
                else if (parsingState == PrepareResponseParseState.Should_End)
                {
                    //reset
                    this._parser.Reset();
                    return;
                }
            }
        }
        void Parse()
        {
            switch (parsingState)
            {
                case PrepareResponseParseState.ExpectedOkPreparePacket:
                    {
                        ParseOkPrepareHeader();
                        break;
                    }
                case PrepareResponseParseState.OkPrepare_Content:
                    {
                        ParseOkPrePareContent();
                        parsingState = PrepareResponseParseState.Expect_ParamsFieldHeader;
                        _tableHeader = new TableHeader();
                        break;
                    }
                case PrepareResponseParseState.Expect_ParamsFieldHeader:
                    {
                        if (_okPrepare.num_params > 0)
                        {
                            _currentHeader = PacketHeader.Empty;
                            if (!_parser.Ensure(PACKET_HEADER_LENGTH))
                            {
                                _needMoreBuffer = true;
                                return;
                            }
                            ParseFieldHeader();
                            if ((_currentPacket != null) && (_currentPacket is EofPacket))
                            {
                                parsingState = PrepareResponseParseState.Params_EOF;
                                break;
                            }
                            parsingState = PrepareResponseParseState.ParamsField_Content;
                            break;
                        }
                        else
                        {
                            parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                        }
                        break;
                    }
                case PrepareResponseParseState.ParamsField_Content:
                    {
                        if (!_parser.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreBuffer = true;
                            return;
                        }
                        ParseFieldPacket();
                        parsingState = PrepareResponseParseState.Expect_ParamsFieldHeader;
                        break;
                    }
                case PrepareResponseParseState.Params_EOF:
                    {
                        ParseEOFPacket();
                        if (_currentPacket != null)
                        {
                            _finalResult = new MySqlPrepareResponse(_okPrepare, _tableHeader);
                            _tableHeader = new TableHeader();
                            parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                        }
                        break;
                    }
                case PrepareResponseParseState.Expect_ColumnsFieldHeader:
                    {
                        if (_okPrepare.num_columns > 0)
                        {
                            _currentHeader = PacketHeader.Empty;
                            if (!_parser.Ensure(PACKET_HEADER_LENGTH))
                            {
                                _needMoreBuffer = true;
                                return;
                            }
                            ParseFieldHeader();
                            if ((_currentPacket != null) && (_currentPacket is EofPacket))
                            {
                                parsingState = PrepareResponseParseState.ColumnsEOF;
                                break;
                            }
                            parsingState = PrepareResponseParseState.ColumnsField_Content;
                        }
                        else
                        {
                            parsingState = PrepareResponseParseState.Should_End;
                        }
                        break;
                    }
                case PrepareResponseParseState.ColumnsField_Content:
                    {
                        if (!_parser.Ensure(_currentHeader.ContentLength))
                        {
                            _needMoreBuffer = true;
                            return;
                        }
                        ParseFieldPacket();
                        parsingState = PrepareResponseParseState.Expect_ColumnsFieldHeader;
                        break;
                    }
                case PrepareResponseParseState.ColumnsEOF:
                    {
                        ParseEOFPacket();
                        if (_currentPacket != null)
                        {
                            _finalResult = new MySqlPrepareResponse(_okPrepare, _tableHeader);
                            parsingState = PrepareResponseParseState.Should_End;
                        }
                        break;
                    }
                case PrepareResponseParseState.Should_End:
                    {
                        break;
                    }
                case PrepareResponseParseState.Error_Content:
                    {
                        ParseErrorPacket();
                        parsingState = PrepareResponseParseState.Should_End;
                        break;
                    }
                default:
                    {
                        parsingState = PrepareResponseParseState.Should_End;
                        break;
                    }
            }
        }
        void ParseOkPrepareHeader()
        {
            _currentHeader = PacketHeader.Empty;
            if (!_parser.Ensure(PACKET_HEADER_LENGTH + 1))
            {
                _needMoreBuffer = true;
                return;
            }
            _currentHeader = _parser.ParsePacketHeader();
            byte type = _parser.PeekByte();
            switch (type)
            {
                case ERROR_CODE:
                    {
                        ParseErrorPacket();
                        parsingState = PrepareResponseParseState.Should_End;
                    }
                    break;
                case EOF_CODE:
                case OK_CODE:
                    {
                        parsingState = PrepareResponseParseState.OkPrepare_Content;
                    }
                    break;
                default:
                    {
                        parsingState = PrepareResponseParseState.Should_End;
                        throw new NotSupportedException("Packet type don't match!!");
                    }
            }
        }
        void ParseOkPrePareContent()
        {
            if (!_parser.Ensure(_currentHeader.ContentLength))
            {
                _needMoreBuffer = true;
                return;
            }
            OkPrepareStmtPacket okPrepare = new OkPrepareStmtPacket();
            okPrepare.Header = _currentHeader;
            okPrepare.ParsePacket(_parser);
            this._okPrepare = okPrepare;
        }

        void ParseErrorPacket()
        {
            if (!_parser.Ensure(_currentHeader.ContentLength))
            {
                _needMoreBuffer = true;
                parsingState = PrepareResponseParseState.Error_Content;
                return;
            }
            var errPacket = new ErrPacket();
            errPacket.Header = _currentHeader;
            uint packetLen = errPacket.GetPacketLength();
            _currentPacket = errPacket;
            errPacket.ParsePacket(_parser);
            //------------------------
            this._finalResult = new MySqlError(errPacket);
        }

        void ParseEOFPacket()
        {
            _currentPacket = null;
            if (!_parser.Ensure(_currentHeader.ContentLength))
            {
                _needMoreBuffer = true;
                return;
            }
            EofPacket eofPacket = new EofPacket(this._isProtocol41);
            eofPacket.Header = _currentHeader;
            _currentPacket = eofPacket;
            eofPacket.ParsePacket(_parser);
        }

        void ParseFieldHeader()
        {
            _currentHeader = _parser.ParsePacketHeader();
            _currentPacket = null;
            if (_parser.PeekByte() == EOF_CODE)
            {
                _currentPacket = new EofPacket(_isProtocol41);
            }
        }
        void ParseFieldPacket()
        {
            FieldPacket field = new FieldPacket(_isProtocol41);
            field.Header = _currentHeader;
            field.ParsePacket(_parser);
            if (_tableHeader != null)
            {
                _tableHeader.AddField(field);
            }
        }
    }

    class MySqlConnectionPacketParser : MySqlPacketParser
    {
        PacketParser _parser = new PacketParser(Encoding.UTF8);
        HandshakePacket _handshake;
        MySqlHandshakeResult _finalResult;
        public MySqlConnectionPacketParser()
        {
        }
        public override MySqlResult ResultPacket
        {
            get
            {
                return _finalResult;
            }
        }

        public override bool NeedMoreBuffer
        {
            get
            {
                //TODO: review here
                //should complete at once ?
                return false;
            }
        }

        public override void Parse(byte[] buffer, int count)
        {
            _finalResult = null;
            //1.create connection frame  
            //_writer.Reset();  
            _parser.LoadNewBuffer(buffer, count);
            _handshake = new HandshakePacket();
            _handshake.ParsePacket(_parser);
            _finalResult = new MySqlHandshakeResult(_handshake);
        }
    }


    /// <summary>
    /// mysql parser manager
    /// </summary>
    class MySqlParserMx : IDisposable
    {
        MemoryStream ms;
        MySqlPacketParser currentPacketParser; //current parser
        PacketWriter _writer;
        bool _isCompleted;
        public MySqlParserMx(PacketWriter _writer)
        {
            ms = new MemoryStream();
            this._writer = _writer;
        }
        public MySqlResult ResultPacket
        {
            get;
            private set;
        }
        public MySqlPacketParser CurrentPacketParser
        {
            get { return currentPacketParser; }
            set
            {
                ResultPacket = null;
                currentPacketParser = value;
            }
        }
        public bool IsComplete
        {
            get { return _isCompleted; }
        }

        public void ParseData(RecvIO recvIO)
        {
            //we need to parse some data here 
            //load incomming data into ms 
            //load data from recv buffer into the ms
            //---------------
            //copy all to stream
            //---------------  
            int maxBuffer = 265000;
            byte[] buffer = new byte[maxBuffer];
            int count = recvIO.BytesTransferred;
            if (count > 0)
            {
                if (count > maxBuffer)
                {
                    throw new Exception();
                }
                try
                {
                    //start index always 0
                    recvIO.ReadTo(0, buffer, count);
                }
                catch (Exception)
                {
                    count = 0;
                }
            }
            //may not complete in first round *** 
            currentPacketParser.Parse(buffer, count);
            ResultPacket = currentPacketParser.ResultPacket;
            if (currentPacketParser.NeedMoreBuffer)
            {
                //***
                //TODO: review here
                _isCompleted = false;
                recvIO.StartReceive();
            }
            else
            {
                _isCompleted = true;
            }
            //--------------------
            //not need to wait here
            //just return *** 
            //--------------------
        }
        public void Dispose()
        {
            if (ms != null)
            {
                ms.Close();
                ms.Dispose();
                ms = null;
            }
        }
    }

}
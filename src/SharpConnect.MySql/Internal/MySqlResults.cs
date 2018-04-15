//LICENSE: MIT
//Copyright(c) 2012 Felix Geisendörfer(felix @debuggable.com) and contributors 
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

using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    enum MySqlResultKind
    {
        Handshake,
        Error,
        Ok,
        PrepareResponse,
        TableResult,
        MultiTableResult,
    }
    abstract class MySqlResult
    {
        public bool IsError { get; protected set; }
        public abstract MySqlResultKind Kind { get; }
    }
    class MySqlHandshakeResult : MySqlResult
    {
        public readonly HandshakePacket packet;
        public MySqlHandshakeResult(HandshakePacket packet)
        {
            this.packet = packet;
        }
        public override MySqlResultKind Kind { get { return MySqlResultKind.Handshake; } }
    }
    class MySqlErrorResult : MySqlResult
    {
        public readonly ErrPacket errPacket;
        public MySqlErrorResult(ErrPacket errPacket)
        {
            this.errPacket = errPacket;
            this.IsError = true;
        }
        public override MySqlResultKind Kind { get { return MySqlResultKind.Error; } }
#if DEBUG
        public override string ToString()
        {
            return errPacket.message;
        }
#endif
    }
    class MySqlOkResult : MySqlResult
    {
        public readonly OkPacket okpacket;
        public MySqlOkResult(OkPacket okpacket)
        {
            this.okpacket = okpacket;
        }
        public override string ToString()
        {
            return "<Insert ID : " + okpacket.insertId + " >";
        }
        public override MySqlResultKind Kind { get { return MySqlResultKind.Ok; } }
    }


    class MySqlPrepareResponseResult : MySqlResult
    {
        public readonly OkPrepareStmtPacket okPacket;
        public readonly TableHeader tableHeader;
        public MySqlPrepareResponseResult(OkPrepareStmtPacket okPrepare, TableHeader tableHeader)
        {
            this.okPacket = okPrepare;
            this.tableHeader = tableHeader;
        }
        public override MySqlResultKind Kind { get { return MySqlResultKind.PrepareResponse; } }
    }

    class MySqlTableResult : MySqlResult
    {
        public readonly TableHeader tableHeader;
        public readonly List<DataRowPacket> rows;

        public MySqlTableResult(TableHeader tableHeader, List<DataRowPacket> rows)
        {
            this.tableHeader = tableHeader;
            this.rows = rows;
        }
        /// <summary>
        /// this is not the last table of result, It has one or more follower table
        /// </summary>
        public bool HasFollower { get; set; }
        public override MySqlResultKind Kind { get { return MySqlResultKind.TableResult; } }
    }

    class MySqlMultiTableResult : MySqlResult
    {
        public readonly List<MySqlTableResult> subTables = new List<MySqlTableResult>();
        public MySqlMultiTableResult()
        {
        }
        public void AddTableResult(MySqlTableResult table)
        {
            subTables.Add(table);
        }
        public override MySqlResultKind Kind { get { return MySqlResultKind.MultiTableResult; } }
    }
}
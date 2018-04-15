//MIT, 2015-2018, brezza92, EngineKit and contributors 

namespace SharpConnect.MySql.Internal
{
    //-------------------------
    //server capability flags
    //https://dev.mysql.com/doc/internals/en/multi-resultset.html
    //-------------------------
    [System.Flags]
    /// <summary>
    /// the capability flags are used by the client and server to indicate which features they support and want to use. 
    /// </summary>
    enum ServerCapabilityFlags : uint
    {
        /// <summary>
        /// Use the improved version of Old Password Authentication. (Assumed to be set since 4.1.1)
        /// </summary>
        CLIENT_LONG_PASSWORD = 1 << 0,// 0x00000001,
        /// <summary>
        /// Send found rows instead of affected rows in EOF_Packet.
        /// </summary>
        CLIENT_FOUND_ROWS = 1 << 1, //0x00000002,
        /// <summary>
        /// Longer flags in Protocol::ColumnDefinition320.
        /// Server: Supports longer flags. 
        /// Client: Expects longer flags. 
        /// </summary>
        CLIENT_LONG_FLAG = 1 << 2,//0x00000004,
        /// <summary>
        ///  Database (schema) name can be specified on connect in Handshake Response Packet.
        ///  Server: Supports schema-name in Handshake Response Packet. 
        ///  Client: Handshake Response Packet contains a schema-name.
        /// </summary>
        CLIENT_CONNECT_WITH_DB = 1 << 3,//0x00000008,
        /// <summary>
        /// Server: Do not permit database.table.column. 
        /// </summary>
        CLIENT_NO_SCHEMA = 1 << 4,//0x00000010,  
        /// <summary>
        /// Compression protocol supported.
        /// Server:Supports compression. 
        /// Client:Switches to Compression compressed protocol after successful authentication. 
        /// </summary>
        CLIENT_COMPRESS = 1 << 5, //0x00000020, TODO: consider implement this ?
        //Special handling of ODBC behavior.
        //Note: No special behavior since 3.22.
        CLIENT_ODBC = 1 << 6,// 0x00000040,
        /// <summary>
        /// Can use LOAD DATA LOCAL.
        /// Server: Enables the LOCAL INFILE request of LOAD DATA|XML. 
        /// Client: Will handle LOCAL INFILE request.
        /// </summary>
        CLIENT_LOCAL_FILES = 1 << 7,// 0x00000080,
        /// <summary>
        /// Server:  Parser can ignore spaces before '('. 
        /// Client: Let the parser ignore spaces before '('.
        /// </summary>
        CLIENT_IGNORE_SPACE = 1 << 8,// 0x00000100,
        /// <summary>
        /// Server:Supports the 4.1 protocol. 
        /// Client: Uses the 4.1 protocol. 
        /// Note: this value was CLIENT_CHANGE_USER in 3.22, unused in 4.0
        /// </summary>
        CLIENT_PROTOCOL_41 = 1 << 9, //0x00000200,
        /// <summary>
        /// wait_timeout versus wait_interactive_timeout.
        /// Server: Supports interactive and noninteractive clients.
        /// Client: Client is interactive.
        /// </summary>
        CLIENT_INTERACTIVE = 1 << 10,
        /// <summary>
        /// Server: Support SSL,
        /// Client: Switch to SSL after sending the capability-flags. 
        /// </summary>
        CLIENT_SSL = 1 << 11,//0x00000800,//TODO: consider implement this

        /// <summary>
        /// Client:Do not issue SIGPIPE if network failures occur (libmysqlclient only).          
        /// </summary>
        CLIENT_IGNORE_SIGPIPE = 1 << 12,//0x00001000,
        /// <summary>
        /// Server: Can send status flags in EOF_Packet.
        /// Client: Expects status flags in EOF_Packet. 
        /// Note: This flag is optional in 3.23, but always set by the server since 4.0.
        /// </summary>
        CLIENT_TRANSACTIONS = 1 << 13,//0x00002000,
        /// <summary>
        /// Unused
        /// Note:Was named CLIENT_PROTOCOL_41 in 4.1.0.
        /// </summary>
        CLIENT_RESERVED = 1 << 14,// 0x00004000,
        /// <summary>
        /// Server: Supports Authentication::Native41. 
        /// Client: Supports Authentication::Native41. 
        /// </summary>
        CLIENT_SECURE_CONNECTION = 1 << 15,// 0x00008000,
        /// <summary>
        /// Server: Can handle multiple statements per COM_QUERY and COM_STMT_PREPARE. 
        /// Client: May send multiple statements per COM_QUERY and COM_STMT_PREPARE. 
        /// Note: Was named CLIENT_MULTI_QUERIES in 4.1.0, renamed later.
        /// Require: CLIENT_PROTOCOL_41 
        /// </summary>
        CLIENT_MULTI_STATEMENTS = 1 << 16,// 0x00010000,
        /// <summary>
        /// Server: Can send multiple resultsets for COM_QUERY. 
        /// Client: Can handle multiple resultsets for COM_QUERY. 
        /// Require: CLIENT_PROTOCOL_41 
        /// </summary>
        CLIENT_MULTI_RESULTS = 1 << 17,// 0x00020000,
        /// <summary>
        /// Server: Can send multiple resultsets for COM_STMT_EXECUTE. 
        /// Client: Can handle multiple resultsets for COM_STMT_EXECUTE. 
        /// Require: CLIENT_PROTOCOL_41 
        /// </summary>
        CLIENT_PS_MULTI_RESULTS = 1 << 18,// 0x00040000,
        /// <summary>
        /// Server: Sends extra data in Initial Handshake Packet and supports the pluggable authentication protocol. 
        /// Client: Supports authentication plugins. 
        /// Require: CLIENT_PROTOCOL_41 
        /// </summary>
        CLIENT_PLUGIN_AUTH = 1 << 19, //0x00080000,
        /// <summary>
        /// Server: Permits connection attributes in Protocol::HandshakeResponse41. 
        /// Client: Sends connection attributes in Protocol::HandshakeResponse41. 
        /// </summary>
        CLIENT_CONNECT_ATTRS = 1 << 20,// 0x00100000,
        /// <summary>
        /// Server: Understands length-encoded integer for auth response data in Protocol::HandshakeResponse41. 
        /// Client: Length of auth response data in Protocol::HandshakeResponse41 is a length-encoded integer. 
        /// Note: The flag was introduced in 5.6.6, but had the wrong value.
        /// </summary>
        CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA = 1 << 21,// 0x00200000,
        /// <summary>
        /// Server: Announces support for expired password extension. 
        /// Client: Can handle expired passwords. 
        /// </summary>
        CLIENT_CAN_HANDLE_EXPIRED_PASSWORDS = 1 << 22,//0x00400000,
        /// <summary>
        /// Server: Can set SERVER_SESSION_STATE_CHANGED in the Status Flags and send session-state change data after a OK packet. 
        /// Client: Expects the server to send sesson-state changes after a OK packet. 
        /// </summary>
        CLIENT_SESSION_TRACK = 1 << 23,// 0x00800000,
        /// <summary>
        /// Server: Can send OK after a Text Resultset. 
        /// Client: Expects an OK (instead of EOF) after the resultset rows of a Text Resultset.  
        /// </summary>
        CLIENT_DEPRECATE_EOF = 1 << 24,//0x01000000
        //background for  CLIENT_DEPRECATE_EOF
        //To support CLIENT_SESSION_TRACK, additional information must be sent after all successful commands. Although the OK packet is extensible, the EOF packet is not due to the overlap of its bytes with the content of the Text Resultset Row.
        //Therefore, the EOF packet in the Text Resultset is replaced with an OK packet. EOF packets are deprecated as of MySQL 5.7.5.
    }
}
//MIT, 2015-2019, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
namespace SharpConnect.MySql.Internal
{
    static class ConnectionPool
    {
        static readonly ConnectionPoolAgent s_connPoolAgent = new ConnectionPoolAgent();
        static object s_queueLock = new object();
        public static Connection GetConnection(MySqlConnectionString connstr)
        {
            lock (s_queueLock)
            {
                return s_connPoolAgent.GetConnection(connstr);
            }
        }
        public static void ReleaseConnection(MySqlConnectionString connstr, Connection conn)
        {
            lock (s_queueLock)
            {
                s_connPoolAgent.ReleaseConnection(connstr, conn);
            }
        }
        public static void ClearConnectionPool()
        {
            lock (s_queueLock)
            {
                s_connPoolAgent.ClearAllConnections();
            }
        }




        class ConnectionPoolAgent
        {
            static Dictionary<int, Queue<Connection>> s_connQueue = new Dictionary<int, Queue<Connection>>();
            static object s_connLock = new object();

            public ConnectionPoolAgent()
            {
            }
            ~ConnectionPoolAgent()
            {
                ClearAllConnections();
            }
            public void ClearAllConnections()
            {
                lock (s_connLock)
                {
                    foreach (var q in s_connQueue.Values)
                    {
                        for (int i = s_connQueue.Count - 1; i >= 0; --i)
                        {
                            try
                            {
                                var conn = q.Dequeue();
                                conn.Disconnect();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    s_connQueue.Clear();
                }
            }
            public Connection GetConnection(MySqlConnectionString connstr)
            {
                lock (s_connLock)
                {

                    //not found
                    if (!s_connQueue.TryGetValue(connstr.ConnSignature, out Queue<Connection> found))
                    {
                        return null;
                    }

                    if (found.Count > 0)
                    {
                        Connection conn = found.Dequeue();
                        //TODO: check if conn is valid
                        conn.IsStoredInConnPool = false;
                        return conn.State == ConnectionState.Connected ? conn : null;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            public void ReleaseConnection(MySqlConnectionString connstr, Connection conn)
            {
                lock (s_connLock)
                {

                    //not found
                    if (!s_connQueue.TryGetValue(connstr.ConnSignature, out Queue<Connection> found))
                    {
                        found = new Queue<Connection>();
                        s_connQueue.Add(connstr.ConnSignature, found);
                    }
                    conn.IsStoredInConnPool = true;
                    found.Enqueue(conn);
                }
            }
        }
    }
}
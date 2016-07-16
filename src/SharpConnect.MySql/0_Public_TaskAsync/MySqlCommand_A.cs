//MIT, 2016, brezza92, EngineKit and contributors

using System;
using System.Threading.Tasks;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    partial class MySqlCommand
    {
        public Task PrepareAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            Prepare(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public Task ExecuteNonQueryAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            ExecuteNonQuery(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public Task<MySqlDataReader> ExecuteReaderAsync()
        {
            var tcs = new TaskCompletionSource<MySqlDataReader>();
            ExecuteReader(exec_reader => tcs.SetResult(exec_reader));
            return tcs.Task;
        }
    }
}
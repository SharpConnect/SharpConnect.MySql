//MIT, 2016, brezza92, EngineKit and contributors

using System;
using System.Threading.Tasks;
using SharpConnect.MySql.Internal;

namespace SharpConnect.MySql
{
    partial class MySqlConnection
    {
        public Task OpenAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            Open(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public Task CloseAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            Close(() => tcs.SetResult(0));
            return tcs.Task;
        }
        

    }
}
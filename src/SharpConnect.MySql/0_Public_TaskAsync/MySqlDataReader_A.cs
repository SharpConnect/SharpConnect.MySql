//MIT, 2016, brezza92, EngineKit and contributors
 
using System.Threading.Tasks; 
namespace SharpConnect.MySql
{

    partial class MySqlDataReader
    {
        public Task CloseAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            Close(() => tcs.SetResult(0));
            return tcs.Task;
        }
    }

}
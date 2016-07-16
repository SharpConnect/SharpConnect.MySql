//MIT, 2016, brezza92, EngineKit and contributors


using System.Threading.Tasks;
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
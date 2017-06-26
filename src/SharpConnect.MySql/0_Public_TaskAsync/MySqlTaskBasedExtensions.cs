//MIT, 2016, brezza92, EngineKit and contributors  

using System.Threading.Tasks;
using SharpConnect.MySql.AsyncPatt;
namespace SharpConnect.MySql
{
    public static class MySqlTaskBasedExtensions
    {
        public static Task OpenAsync(this MySqlConnection conn)
        {
            var tcs = new TaskCompletionSource<int>();
            conn.InternalOpen(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public static Task CloseAsync(this MySqlConnection conn)
        {
            var tcs = new TaskCompletionSource<int>();
            conn.Close(() => tcs.SetResult(0));
            return tcs.Task;
        }
        //------------------------------------------------------------
        public static Task PrepareAsync(this MySqlCommand cmd)
        {
            var tcs = new TaskCompletionSource<int>();
            cmd.Prepare(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public static Task ExecuteNonQueryAsync(this MySqlCommand cmd)
        {
            var tcs = new TaskCompletionSource<int>();
            cmd.ExecuteNonQuery(() => tcs.SetResult(0));
            return tcs.Task;
        }
        public static Task<MySqlDataReader> ExecuteReaderAsync(this MySqlCommand cmd)
        {
            var tcs = new TaskCompletionSource<MySqlDataReader>();
            cmd.ExecuteReader(exec_reader => tcs.SetResult(exec_reader));
            return tcs.Task;
        }
        //-----------------------------------------------------------------------------
        public static Task CloseAsync(this MySqlDataReader reader)
        {
            var tcs = new TaskCompletionSource<int>();
            reader.Close(() => tcs.SetResult(0));
            return tcs.Task;
        }
    }
}
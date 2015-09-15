//MIT 2015, brezza27, EngineKit and contributors


namespace SharpConnect.MySql.Utils
{
    public interface IHasParameters
    {
        CommandParams Pars { get; }
    }

    public static class SimppleCommandExtensions
    {
        public static void AddWithValue(this IHasParameters ihasPars, string key, string value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, byte value)
        {
            ihasPars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, short value)
        {
            ihasPars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, int value)
        {
            ihasPars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, long value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, float value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, double value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, decimal value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, byte[] value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, System.DateTime value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, sbyte value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, char value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, ushort value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, uint value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void AddWithValue(this IHasParameters ihasPars, string key, ulong value)
        {
            ihasPars.Pars.AddWithValue(key, value);
        }
        public static void ClearValues(this IHasParameters ihasPars)
        {
            ihasPars.Pars.ClearDataValues();
        }

    }
}

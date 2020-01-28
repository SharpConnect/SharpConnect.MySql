
namespace SharpConnect.MySql.Internal
{
    static class MySqlStringToHexUtils
    {
        //-------------------------------------------------------
        //convert byte array to binary
        //from http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/24343727#24343727
        static readonly uint[] s_lookup32 = CreateLookup32();
        static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        public static void ConvertByteArrayToHexWithMySqlPrefix(byte[] bytes, System.Text.StringBuilder stbuilder)
        {
            //for mysql only !, 
            //we prefix with 0x

            var lookup32 = s_lookup32;
            int j = bytes.Length;
            var result = new char[(j * 2) + 2];
            int m = 0;
            result[0] = '0';
            result[1] = 'x';
            m = 2;
            for (int i = 0; i < j; i++)
            {
                uint val = lookup32[bytes[i]];
                result[m] = (char)val;
                result[m + 1] = (char)(val >> 16);
                m += 2;
            }

            stbuilder.Append(result);
        }
    }
}
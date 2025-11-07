using System.Text;

namespace DaJet
{
    public static class DbUtilities
    {
        public static int GetInt32(byte[] bytes)
        {
            byte[] value = new byte[4];
            bytes.CopyTo(value, 0);
            if (BitConverter.IsLittleEndian) Array.Reverse(value);
            return BitConverter.ToInt32(value, 0);
        }
        public static long GetInt64(byte[] bytes)
        {
            byte[] value = new byte[8];
            bytes.CopyTo(value, 0);
            if (BitConverter.IsLittleEndian) Array.Reverse(value);
            return BitConverter.ToInt64(value, 0);
        }
        public static ulong GetUInt64(byte[] bytes)
        {
            byte[] value = new byte[8];
            bytes.CopyTo(value, 0);
            if (BitConverter.IsLittleEndian) Array.Reverse(value);
            return BitConverter.ToUInt64(value, 0);
        }
        public static byte[] GetByteArray(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }
        public static byte[] GetByteArray(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new(ba.Length * 2);

            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
        public static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;

            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public static byte[] Get1CUuid(byte[] uuid_sql)
        {
            // CREATE OR ALTER FUNCTION [dbo].[fn_sql_to_1c_uuid] (@uuid_sql binary(16))
            // RETURNS nvarchar(36)
            // AS
            // BEGIN
            // DECLARE @uuid_1c binary(16) = CAST(REVERSE(SUBSTRING(@uuid_sql, 9, 8)) AS binary(8)) + SUBSTRING(@uuid_sql, 1, 8);
            // RETURN CAST(CAST(@uuid_1c AS uniqueidentifier) AS nvarchar(36));
            // END;

            byte[] uuid_1c = new byte[16];

            for (int i = 0; i < 8; i++)
            {
                uuid_1c[i] = uuid_sql[15 - i];
                uuid_1c[8 + i] = uuid_sql[i];
            }

            return uuid_1c;
        }
        public static byte[] Get1CUuid(Guid guid_sql)
        {
            return Get1CUuid(guid_sql.ToByteArray());
        }
        public static byte[] GetSqlUuid(byte[] uuid_1c)
        {
            byte[] uuid_sql = new byte[16];

            for (int i = 0; i < 8; i++)
            {
                uuid_sql[i] = uuid_1c[8 + i];
                uuid_sql[8 + i] = uuid_1c[7 - i];
            }

            return uuid_sql;
        }
        public static byte[] GetSqlUuid(Guid guid_1c)
        {
            return GetSqlUuid(guid_1c.ToByteArray());
        }
    }
}
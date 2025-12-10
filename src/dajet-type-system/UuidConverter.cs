namespace DaJet.TypeSystem
{
    public static class UuidConverter
    {
        public static byte[] Get1CUuid(byte[] uuid_db)
        {
            // CREATE OR ALTER FUNCTION [dbo].[fn_db_to_1c_uuid] (@uuid_db binary(16))
            // RETURNS nvarchar(36)
            // AS
            // BEGIN
            // DECLARE @uuid_1c binary(16) = CAST(REVERSE(SUBSTRING(@uuid_db, 9, 8)) AS binary(8)) + SUBSTRING(@uuid_db, 1, 8);
            // RETURN CAST(CAST(@uuid_1c AS uniqueidentifier) AS nvarchar(36));
            // END;

            byte[] uuid_1c = new byte[16];

            for (int i = 0; i < 8; i++)
            {
                uuid_1c[i] = uuid_db[15 - i];
                uuid_1c[8 + i] = uuid_db[i];
            }

            return uuid_1c;
        }
        public static byte[] Get1CUuid(Guid guid_db)
        {
            return Get1CUuid(guid_db.ToByteArray());
        }

        public static byte[] GetDbUuid(byte[] uuid_1c)
        {
            byte[] uuid_db = new byte[16];

            for (int i = 0; i < 8; i++)
            {
                uuid_db[i] = uuid_1c[8 + i];
                uuid_db[8 + i] = uuid_1c[7 - i];
            }

            return uuid_db;
        }
        public static byte[] GetDbUuid(Guid guid_1c)
        {
            return GetDbUuid(guid_1c.ToByteArray());
        }

        [Function("UUID1C")] public static Guid CONVERT_UUID_DB_TO_1C(Guid uuid)
        {
            return new Guid(Get1CUuid(uuid));
        }
        [Function("UUID1C")] public static Guid CONVERT_UUID_DB_TO_1C(in byte[] source)
        {
            if (source is null || source.Length != 16)
            {
                return Guid.Empty;
            }

            Guid uuid = new(source);

            return new Guid(Get1CUuid(uuid));
        }
        [Function("UUID1C")] public static Guid CONVERT_UUID_DB_TO_1C(in string source)
        {
            if (!Guid.TryParse(source, out Guid uuid))
            {
                return Guid.Empty;
            }

            return new Guid(Get1CUuid(uuid));
        }
        [Function("UUID1C")] public static Guid CONVERT_UUID_DB_TO_1C(Entity entity)
        {
            return new Guid(Get1CUuid(entity.Identity));
        }

        [Function("UUIDDB")] public static Guid CONVERT_UUID_1C_TO_DB(Guid uuid)
        {
            return new Guid(GetDbUuid(uuid));
        }
        [Function("UUIDDB")] public static Guid CONVERT_UUID_1C_TO_DB(in byte[] source)
        {
            if (source is null || source.Length != 16)
            {
                return Guid.Empty;
            }

            Guid uuid = new(source);

            return new Guid(GetDbUuid(uuid));
        }
        [Function("UUIDDB")] public static Guid CONVERT_UUID_1C_TO_DB(in string source)
        {
            if (!Guid.TryParse(source, out Guid uuid))
            {
                return Guid.Empty;
            }

            return new Guid(GetDbUuid(uuid));
        }
        [Function("UUIDDB")] public static Guid CONVERT_UUID_1C_TO_DB(Entity entity)
        {
            return new Guid(GetDbUuid(entity.Identity));
        }
    }
}
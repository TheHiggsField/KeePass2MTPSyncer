using System;
using System.Linq;
using System.Security.Cryptography;
using KeePassLib;

namespace MTPSync
{
    public static class PwDatabaseExtension
    {
        private const string DatabasePublicUUIDKey = @"MTPSync_DatabasePublicUUID";

        public static Guid GetDatabasePublicGuid(this PwDatabase pwDatabase)
        {
            if (!pwDatabase.IsOpen)
                throw new Exception("Can't get the DatabasePublicGuid, if the Password DB isn't open.");

            using (SHA256 sha2 = SHA256.Create())
            {
                var hash = sha2.ComputeHash(pwDatabase.RootGroup.Uuid.UuidBytes);

                if (hash == null)
                    return Guid.Empty;

                return new Guid(hash.Take(16).ToArray());
            }
        }

        public static void SetDatabasePublicGuid(this PwDatabase pwDatabase)
        {
            pwDatabase.PublicCustomData.SetByteArray(DatabasePublicUUIDKey, pwDatabase.GetDatabasePublicGuid().ToByteArray());
        }

        public static Guid ReadDatabasePublicGuid(this PwDatabase pwDatabase)
        {
            byte[] bytes = pwDatabase.PublicCustomData.GetByteArray(DatabasePublicUUIDKey);

            if (bytes == null)
                return Guid.Empty;

            return new Guid(bytes);
        }
    }
}

using System;
using System.Linq;
using System.Security.Cryptography;
using KeePassLib;
using KeePassLib.Security;

namespace LocalSync.Extension
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

        public static ProtectedString GetSharedKey(this PwDatabase pwDatabase, string profileName)
        {
            var fileShareGroup = pwDatabase.RootGroup.Groups
                .FirstOrDefault(grp => grp.Name == "FileShare");

            if (fileShareGroup == null)
                return null;


            var sharedkeyEntry = fileShareGroup.Entries.FirstOrDefault(
                    ent => ent.Strings.ReadSafe(PwDefs.TitleField) == profileName
                );

            if (sharedkeyEntry == null)
                return null;

            var sharedKey2 = sharedkeyEntry?.Strings;

            var sharedKey = sharedkeyEntry?.Strings.GetSafe(PwDefs.PasswordField);
            
            return sharedKey;
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

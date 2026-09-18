using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WaesesUI;

public static class SecureSessionHelper
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory,
        "Config", "Files", "session.dat"
    );

    public static void SaveSession(UserSession session)
    {
        try
        {
            var sessionObj = new
            {
                Email = session.Email,
                Token = session.Token,
                RefreshToken = session.RefreshToken,
                ExpireIn = session.ExpireIn,
                Roles = session.Roles,
                UserId = session.UserId,
                NomComplet = session.NomComplet
            };
            string json = JsonSerializer.Serialize(sessionObj);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);

            byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(FilePath, encryptedBytes);
        }
        catch (Exception)
        {
            // Échec silencieux (ex: problèmes de permissions d'écriture)
        }
    }

    public static UserSession? LoadSession()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;

            byte[] encryptedBytes = File.ReadAllBytes(FilePath);

            // Déchiffrement DPAPI (impossible à lire depuis une autre session Windows ou un autre PC)
            byte[] plaintextBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(plaintextBytes);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Deserialize<UserSession>();

            return root;
        }
        catch (Exception)
        {
            return null; // Fichier corrompu ou clé utilisateur différente
        }
    }

    public static void ClearSession()
    {
        if (File.Exists(FilePath))
        {
            var directory = Path.Combine(AppContext.BaseDirectory,
        "Config", "Files");
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
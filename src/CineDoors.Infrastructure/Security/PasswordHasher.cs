using System.Security.Cryptography;

namespace CineDoors.Infrastructure.Security;

// Calcul et vérification du hash ("empreinte" du mdp en fr)
public class PasswordHasher
{
    private const int SaltSize = 16; // taille en octet du sel (pour hash, enregistré en base pour comparer ensuite à la connexion)
    private const int HashSize = 32; // taille en octet du hash final
    private const int Iterations = 600000; // Nb de répétitions du calcul

    // Premier calcul à la création du compte, renvoie le texte à stocker (sous forme "iterations.sel.empreinte")
    public string Hash(string password)
    {
        // Sel = suite random d'octets, avec laquelle on va transformer le mdp ensuite. Unique pour chaque compte (au cas où X utilisateurs ont le même mdp)
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Calcul du hash, sens unique (mdp + sel -> hash)
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        // Transformation en base64 avant d'enregistrer (// Base64 : écrit les octets en texte, pour une colonne de type texte, pour que ça s'enregistre proprement)
        return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    // -----------------

    // X appels suivants à la connexion, vérification du hash grâce au mdp tapé et au sel récupéré
    // Renvoie vrai si les deux hash correspondent

    public bool Verify(string password, string storedHash)
    {
        // Récupération des infos (format "iterations.sel.empreinte", en base64 pour sel et le hash) et affectation dans variables
        string[] parts = storedHash.Split('.');

        if (parts.Length != 3)
        {
            return false;
        }

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expectedHash = Convert.FromBase64String(parts[2]);

        // Recalcul du mdp pour créer un nouveau hash qu'il faudra comparer à l'ancien
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        // Comparaison avec ancien hash en "temps constant" (tous les octets sont comparés, même si l'un est faux avant la fin. Evite de donner des indices en cas d'attaque
        // sur quel octet est en erreur à quel moment, permettant ainsi de deviner quoi modifier et où, jusqu'à trouver le hash entier).
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
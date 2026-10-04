using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Data;
using CineDoors.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CineDoors.Infrastructure.Services;

// Création de compte et connexion
public class AccountService
{
    private const int MinPasswordLength = 8;

    private readonly CineDoorsDbContext _context;
    private readonly PasswordHasher _hasher;

    // connexion à la base et hash géré autre part pour garder le tout segmenté
    public AccountService(CineDoorsDbContext context, PasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    // --- Création du compte, renvoie null si ok, sinon msg d'erreur ---
    public async Task<string?> RegisterAsync(string username, string email, string password)
    {
        username = username.Trim(); // .Trim pour enlever les espaces aux extremitésn
        email = email.Trim();

        // Checkup des saisies et gestion succinte erreurs
        if (username.Length == 0 || username.Length > 30)
        {
            return "Le pseudo doit contenir entre 1 et 30 caractères.";
        }
        if (!email.Contains('@')) // Si ne contient pas d'arobase
        {
            return "L'adresse e-mail n'est pas valide.";
        }
        if (password.Length < MinPasswordLength)
        {
            return "Le mot de passe doit contenir au moins " + MinPasswordLength + " caractères.";
        }

        // Unicité du pseudo et de l'e-mail. AnyAsync = True si au moins une ligne match les conditions
        bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == username); // await : attend le résultat sans bloquer l'interface
        if (usernameTaken)
        {
            return "Ce pseudo est déjà utilisé.";
        }

        bool emailTaken = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailTaken)
        {
            return "Cette adresse e-mail est déjà utilisée.";
        }

        // --- Enregistrement ---
        AppUser user = new AppUser //Objet "User" créé
        {
            Username = username,
            Email = email,
            PasswordHash = _hasher.Hash(password), // mdp hash stocké
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user); // Prépare l'INSERT
        await _context.SaveChangesAsync(); // INSERT en base

        return null;
    }

    // --- Connecte l'utilisateur ---
    // Si infos pas bonnes, renvoie null. Si ok, renvoie le compte

    public async Task<AppUser?> LoginAsync(string username, string password)
    {
        username = username.Trim();

        AppUser? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username); // FirstOrDefaultAsync() renvoie la première ligne trouvée. null si aucune

        if (user == null)
        {
            return null;
        }

        if (!_hasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<string?> ResetPasswordAsync(string username, string newPassword)
    {
        username = username.Trim();
        if (newPassword.Length < MinPasswordLength)
        {
            return "Le mot de passe doit contenir au moins" + MinPasswordLength + "caractères.";
        }

        AppUser? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return "Aucun compte ne porte ce pseudo.";
        }

        // Maj du hash (ne correspond plus à l'ancien mdp)
        user.PasswordHash = _hasher.Hash(newPassword);
        await _context.SaveChangesAsync(); // EF Core détecte quand l'objet change, SaveChangesAsync vaut donc UPDATE
        
        return null;
    }

}
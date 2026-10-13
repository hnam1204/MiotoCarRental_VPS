using Microsoft.AspNetCore.Identity;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Services;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    PasswordVerificationResult VerifyAndUpgrade(User user, string password);
}

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password) => _hasher.HashPassword(user, password);

    public PasswordVerificationResult VerifyAndUpgrade(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return PasswordVerificationResult.Failed;
        }

        if (user.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = HashPassword(user, password);
            }

            return result;
        }

        // Existing coursework accounts used plaintext and two legacy demo formats.
        var legacyMatch = string.Equals(user.PasswordHash, password, StringComparison.Ordinal)
            || string.Equals(user.PasswordHash, $"{password}_hash_123", StringComparison.Ordinal)
            || string.Equals(user.PasswordHash, $"{password.ToLowerInvariant()}123", StringComparison.Ordinal);

        if (!legacyMatch)
        {
            return PasswordVerificationResult.Failed;
        }

        user.PasswordHash = HashPassword(user, password);
        return PasswordVerificationResult.SuccessRehashNeeded;
    }
}

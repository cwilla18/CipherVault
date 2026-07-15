namespace CipherVault.Core.Data;

public static class Config
{
    /// <summary>Upper bound on prompt re-tries, guarding against infinite loops on redirected/EOF input.</summary>
    public const int MaxPromptAttempts = 100;

    /// <summary>Maximum accepted password length during masked entry.</summary>
    public const int MaxPasswordLength = 1024;

    // At/above the OWASP floor for PBKDF2-SHA256. Stored per-file in the .cwe
    // header, so raising it does not break existing vaults.
    public const int KdfIterations = 700_000;

    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int SaltSize = 16;
}

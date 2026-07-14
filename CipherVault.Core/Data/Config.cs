namespace CipherVault.Core.Data;

public static class Config
{
    public const int WhileLoopSanityCheck = 1000;
    public const int KdfIterations = 100_000;

    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int SaltSize = 16;
}

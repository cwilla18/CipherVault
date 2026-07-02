namespace Encypter.Data
{
    internal static class Config
    {
        public const string MasterPasswordEnvVar = "Cw-Encrypter";
        public const int WhileLoopSanityCheck = 1000;
        public const int Increment = 3;
        public const int ChunkingSize = 4;
        public const string FileTypePath = "/data.dcwe";

        public const int KeySize = 32;
        public const int IVSize = 16;
    }
}

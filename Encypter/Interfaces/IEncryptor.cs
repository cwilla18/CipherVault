using System;
using Encypter.Records;

namespace Encypter.Interfaces;

internal interface IEncryptor
{
    /// <summary>
    /// Encrypt plaintext with the provided key and optional associated data.
    /// Returns plaintext, associatedData and authentication key.
    /// </summary>
    EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key);

    /// <summary>
    /// Decrypt ciphertext using nonce, tag, key and optional associated data.
    /// Returns plaintext bytes; throws if authentication fails.
    /// </summary>
    byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key);

    /// <summary>
    /// Derives a key from a password and salt using PBKDF2 (Rfc2898) with the specified iterations.
    /// </summary>
    byte[] DeriveKeyFromPassword(string password, ReadOnlySpan<byte> salt, int iterations = 100_000);
}

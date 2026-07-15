using CipherVault.Core.Data;
using CipherVault.Core.Interfaces;
using CipherVault.Core.Records;
using System;
using System.Security.Cryptography;

namespace CipherVault.Core;

public sealed class AesGcmCipher : ICipher
{
    public byte[] DeriveKeyFromPassword(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations = 700_000)
    {
        if (salt.Length == 0)
        {
            throw new ArgumentException("Salt must not be empty.", nameof(salt));
        }
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        // Static Pbkdf2 avoids constructing an Rfc2898DeriveBytes that would
        // hold onto the password bytes for the lifetime of the object.
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, Config.KeySize);
    }

    EncryptedPayload ICipher.Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key)
    {
        if (key.Length != Config.KeySize)
        {
            throw new ArgumentException($"Key must be {Config.KeySize} bytes.", nameof(key));
        }

        var nonce = new byte[Config.NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[Config.TagSize];

        using var aes = new AesGcm(key.ToArray(), Config.TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return new EncryptedPayload(nonce, ciphertext, tag);
    }

    byte[] ICipher.Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (key.Length != Config.KeySize)
        {
            throw new ArgumentException($"Key must be {Config.KeySize} bytes.", nameof(key));
        }
        if (payload.Nonce.Length != Config.NonceSize)
        {
            throw new ArgumentException($"Nonce must be {Config.NonceSize} bytes.", nameof(payload));
        }
        if (payload.Tag.Length != Config.TagSize)
        {
            throw new ArgumentException($"Tag must be {Config.TagSize} bytes.", nameof(payload));
        }

        var plaintext = new byte[payload.Ciphertext.Length];

        using var aes = new AesGcm(key.ToArray(), Config.TagSize);
        // Throws AuthenticationTagMismatchException if the tag does not verify.
        aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext, associatedData);

        return plaintext;
    }
}

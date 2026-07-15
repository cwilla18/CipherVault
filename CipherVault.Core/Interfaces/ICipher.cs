using System;
using CipherVault.Core.Records;

namespace CipherVault.Core.Interfaces;

public interface ICipher
{
    EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key);

    byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key);

    byte[] DeriveKeyFromPassword(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations = 700_000);
}

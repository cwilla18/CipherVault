using System;
using System.Buffers.Binary;
using System.IO;
using CipherVault.Core.Data;
using CipherVault.Core.Records;

namespace CipherVault.Core.Formats;

public static class CweFileFormat
{
    private static readonly byte[] Magic = { (byte)'C', (byte)'W', (byte)'E', (byte)'F' };
    private const byte CurrentVersion = 1;

    private static int HeaderLength =>
        Magic.Length + 1 + sizeof(int) + Config.SaltSize + Config.NonceSize + Config.TagSize;

    /// <summary>
    /// Builds the associated data bound to the AES-GCM tag: magic + version +
    /// iterations + salt. Passing this on encrypt and decrypt makes the tag
    /// detect tampering with (or swapping of) a file's header fields.
    /// </summary>
    public static byte[] BuildAssociatedData(byte[] salt, int iterations)
    {
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length != Config.SaltSize)
            throw new ArgumentException($"Salt must be {Config.SaltSize} bytes.", nameof(salt));

        var ad = new byte[Magic.Length + 1 + sizeof(int) + salt.Length];
        var span = ad.AsSpan();
        Magic.CopyTo(span);
        var offset = Magic.Length;
        span[offset++] = CurrentVersion;
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, sizeof(int)), iterations);
        offset += sizeof(int);
        salt.CopyTo(span.Slice(offset));
        return ad;
    }

    public static void Write(Stream output, byte[] salt, int iterations, EncryptedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentNullException.ThrowIfNull(payload);

        if (salt.Length != Config.SaltSize)
            throw new ArgumentException($"Salt must be {Config.SaltSize} bytes.", nameof(salt));
        if (payload.Nonce.Length != Config.NonceSize)
            throw new ArgumentException($"Nonce must be {Config.NonceSize} bytes.", nameof(payload));
        if (payload.Tag.Length != Config.TagSize)
            throw new ArgumentException($"Tag must be {Config.TagSize} bytes.", nameof(payload));

        output.Write(Magic);
        output.WriteByte(CurrentVersion);

        Span<byte> iterationBuffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(iterationBuffer, iterations);
        output.Write(iterationBuffer);

        output.Write(salt);
        output.Write(payload.Nonce);
        output.Write(payload.Tag);
        output.Write(payload.Ciphertext);
    }

    public static CweFile Read(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var buffer = new MemoryStream();
        input.CopyTo(buffer);
        var data = buffer.ToArray();

        if (data.Length < HeaderLength)
            throw new InvalidDataException("File is too small to be a valid .cwe file.");

        var offset = 0;
        for (var i = 0; i < Magic.Length; i++)
        {
            if (data[offset++] != Magic[i])
                throw new InvalidDataException("Not a recognised .cwe file (bad magic bytes).");
        }

        var version = data[offset++];
        if (version != CurrentVersion)
            throw new InvalidDataException($"Unsupported .cwe file version: {version}.");

        var iterations = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));
        offset += sizeof(int);

        var salt = data.AsSpan(offset, Config.SaltSize).ToArray();
        offset += Config.SaltSize;

        var nonce = data.AsSpan(offset, Config.NonceSize).ToArray();
        offset += Config.NonceSize;

        var tag = data.AsSpan(offset, Config.TagSize).ToArray();
        offset += Config.TagSize;

        var ciphertext = data.AsSpan(offset).ToArray();

        return new CweFile(iterations, salt, new EncryptedPayload(nonce, ciphertext, tag));
    }
}

public sealed record CweFile(int Iterations, byte[] Salt, EncryptedPayload Payload);

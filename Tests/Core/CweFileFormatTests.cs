using System;
using System.IO;
using CipherVault.Core.Data;
using CipherVault.Core.Formats;
using CipherVault.Core.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Core;

[TestClass]
public class CweFileFormatTests
{
    private const int Iterations = 100_000;

    private static byte[] Salt() => Filled(Config.SaltSize, 0x11);
    private static EncryptedPayload SamplePayload() => new(
        Nonce: Filled(Config.NonceSize, 0x22),
        Ciphertext: new byte[] { 10, 20, 30, 40, 50 },
        Tag: Filled(Config.TagSize, 0x33));

    private static byte[] Filled(int length, byte value)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    private static byte[] WriteToBytes(byte[] salt, int iterations, EncryptedPayload payload)
    {
        using var stream = new MemoryStream();
        CweFileFormat.Write(stream, salt, iterations, payload);
        return stream.ToArray();
    }

    [TestMethod]
    public void Write_Then_Read_RoundTripsAllFields()
    {
        var salt = Salt();
        var payload = SamplePayload();

        var bytes = WriteToBytes(salt, Iterations, payload);
        var read = CweFileFormat.Read(new MemoryStream(bytes));

        Assert.AreEqual(Iterations, read.Iterations);
        CollectionAssert.AreEqual(salt, read.Salt);
        CollectionAssert.AreEqual(payload.Nonce, read.Payload.Nonce);
        CollectionAssert.AreEqual(payload.Tag, read.Payload.Tag);
        CollectionAssert.AreEqual(payload.Ciphertext, read.Payload.Ciphertext);
    }

    [TestMethod]
    public void Read_BadMagicBytes_ThrowsInvalidData()
    {
        // Large enough to pass the size check, but the leading bytes are not "CWEF".
        var bytes = new byte[128];

        Assert.ThrowsException<InvalidDataException>(
            () => CweFileFormat.Read(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void Read_TruncatedStream_ThrowsInvalidData()
    {
        var bytes = new byte[8]; // shorter than the header

        Assert.ThrowsException<InvalidDataException>(
            () => CweFileFormat.Read(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void Read_UnsupportedVersion_ThrowsInvalidData()
    {
        var bytes = WriteToBytes(Salt(), Iterations, SamplePayload());
        bytes[4] = 0xFF; // version byte sits right after the 4 magic bytes

        Assert.ThrowsException<InvalidDataException>(
            () => CweFileFormat.Read(new MemoryStream(bytes)));
    }

    [TestMethod]
    public void Write_WrongSaltSize_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => WriteToBytes(new byte[Config.SaltSize - 1], Iterations, SamplePayload()));
    }

    [TestMethod]
    public void Write_WrongNonceSize_ThrowsArgumentException()
    {
        var payload = new EncryptedPayload(new byte[Config.NonceSize + 1], new byte[] { 1 }, Filled(Config.TagSize, 0x33));

        Assert.ThrowsException<ArgumentException>(() => WriteToBytes(Salt(), Iterations, payload));
    }

    [TestMethod]
    public void Write_WrongTagSize_ThrowsArgumentException()
    {
        var payload = new EncryptedPayload(Filled(Config.NonceSize, 0x22), new byte[] { 1 }, new byte[Config.TagSize - 1]);

        Assert.ThrowsException<ArgumentException>(() => WriteToBytes(Salt(), Iterations, payload));
    }

    [TestMethod]
    public void Write_NullOutput_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => CweFileFormat.Write(null!, Salt(), Iterations, SamplePayload()));
    }

    [TestMethod]
    public void Read_NullInput_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => CweFileFormat.Read(null!));
    }
}

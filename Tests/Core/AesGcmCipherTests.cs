using System;
using System.Security.Cryptography;
using System.Text;
using CipherVault.Core;
using CipherVault.Core.Data;
using CipherVault.Core.Interfaces;
using CipherVault.Core.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Core;

[TestClass]
public class AesGcmCipherTests
{
    // Encrypt/Decrypt are explicit interface implementations, so everything
    // goes through ICipher rather than the concrete AesGcmCipher.
    private static ICipher NewCipher() => new AesGcmCipher();

    private static byte[] Salt(byte fill = 0x2A)
    {
        var salt = new byte[Config.SaltSize];
        Array.Fill(salt, fill);
        return salt;
    }

    private static byte[] Pw(string password) => Encoding.UTF8.GetBytes(password);

    private static byte[] Key(ICipher cipher, string password = "Str0ng!Pass99", byte saltFill = 0x2A) => cipher.DeriveKeyFromPassword(Pw(password), Salt(saltFill), 1000);

    [TestMethod]
    public void Encrypt_Then_Decrypt_ReturnsOriginalBytes()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var plaintext = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog.");

        var payload = cipher.Encrypt(plaintext, ReadOnlySpan<byte>.Empty, key);
        var result = cipher.Decrypt(payload, ReadOnlySpan<byte>.Empty, key);

        CollectionAssert.AreEqual(plaintext, result);
    }

    [TestMethod]
    public void Encrypt_Then_Decrypt_LargePayload_RoundTrips()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var plaintext = new byte[1_048_576]; // 1 MB
        RandomNumberGenerator.Fill(plaintext);

        var payload = cipher.Encrypt(plaintext, ReadOnlySpan<byte>.Empty, key);
        var result = cipher.Decrypt(payload, ReadOnlySpan<byte>.Empty, key);

        CollectionAssert.AreEqual(plaintext, result);
    }

    [TestMethod]
    public void Decrypt_WithWrongKey_ThrowsAndDoesNotReturnGarbage()
    {
        var cipher = NewCipher();
        var plaintext = Encoding.UTF8.GetBytes("secret");
        var payload = cipher.Encrypt(plaintext, ReadOnlySpan<byte>.Empty, Key(cipher, "PasswordOne1!"));
        var wrongKey = Key(cipher, "PasswordTwo2!");

        Assert.ThrowsException<AuthenticationTagMismatchException>(() => cipher.Decrypt(payload, ReadOnlySpan<byte>.Empty, wrongKey));
    }

    [TestMethod]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var payload = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), ReadOnlySpan<byte>.Empty, key);

        payload.Ciphertext[0] ^= 0xFF; // flip a bit

        Assert.ThrowsException<AuthenticationTagMismatchException>(() => cipher.Decrypt(payload, ReadOnlySpan<byte>.Empty, key));
    }

    [TestMethod]
    public void Decrypt_TamperedTag_Throws()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var payload = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), ReadOnlySpan<byte>.Empty, key);

        payload.Tag[0] ^= 0xFF;

        Assert.ThrowsException<AuthenticationTagMismatchException>(() => cipher.Decrypt(payload, ReadOnlySpan<byte>.Empty, key));
    }

    [TestMethod]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentNonceAndCiphertext()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var plaintext = Encoding.UTF8.GetBytes("same input every time");

        var first = cipher.Encrypt(plaintext, ReadOnlySpan<byte>.Empty, key);
        var second = cipher.Encrypt(plaintext, ReadOnlySpan<byte>.Empty, key);

        CollectionAssert.AreNotEqual(first.Nonce, second.Nonce);
        CollectionAssert.AreNotEqual(first.Ciphertext, second.Ciphertext);
    }

    [TestMethod]
    public void Encrypt_WithWrongKeySize_ThrowsArgumentException()
    {
        var cipher = NewCipher();
        var badKey = new byte[Config.KeySize - 1];

        Assert.ThrowsException<ArgumentException>(() => cipher.Encrypt(new byte[] { 1, 2, 3 }, ReadOnlySpan<byte>.Empty, badKey));
    }

    [TestMethod]
    public void Decrypt_NullPayload_ThrowsArgumentNullException()
    {
        var cipher = NewCipher();

        Assert.ThrowsException<ArgumentNullException>(() => cipher.Decrypt(null!, ReadOnlySpan<byte>.Empty, Key(cipher)));
    }

    [TestMethod]
    public void Decrypt_WrongNonceSize_ThrowsArgumentException()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var good = cipher.Encrypt(new byte[] { 1, 2, 3 }, ReadOnlySpan<byte>.Empty, key);
        var badNonce = new EncryptedPayload(new byte[Config.NonceSize + 1], good.Ciphertext, good.Tag);

        Assert.ThrowsException<ArgumentException>(() => cipher.Decrypt(badNonce, ReadOnlySpan<byte>.Empty, key));
    }

    [TestMethod]
    public void Decrypt_WrongTagSize_ThrowsArgumentException()
    {
        var cipher = NewCipher();
        var key = Key(cipher);
        var good = cipher.Encrypt(new byte[] { 1, 2, 3 }, ReadOnlySpan<byte>.Empty, key);
        var badTag = new EncryptedPayload(good.Nonce, good.Ciphertext, new byte[Config.TagSize + 1]);

        Assert.ThrowsException<ArgumentException>(() => cipher.Decrypt(badTag, ReadOnlySpan<byte>.Empty, key));
    }

    // ---------- key derivation ----------

    [TestMethod]
    public void DeriveKey_SameInputs_ProducesIdenticalKey()
    {
        var cipher = NewCipher();

        var a = cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(), 5000);
        var b = cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(), 5000);

        CollectionAssert.AreEqual(a, b);
    }

    [TestMethod]
    public void DeriveKey_DifferentSalt_ProducesDifferentKey()
    {
        var cipher = NewCipher();

        var a = cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(0x01), 5000);
        var b = cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(0x02), 5000);

        CollectionAssert.AreNotEqual(a, b);
    }

    [TestMethod]
    public void DeriveKey_ReturnsKeyOfConfiguredSize()
    {
        var cipher = NewCipher();

        var key = cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(), 5000);

        Assert.AreEqual(Config.KeySize, key.Length);
    }

    [TestMethod]
    public void DeriveKey_EmptySalt_ThrowsArgumentException()
    {
        var cipher = NewCipher();

        Assert.ThrowsException<ArgumentException>(() => cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Array.Empty<byte>(), 5000));
    }

    [TestMethod]
    public void DeriveKey_NonPositiveIterations_ThrowsArgumentOutOfRangeException()
    {
        var cipher = NewCipher();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => cipher.DeriveKeyFromPassword(Pw("MyP@ssw0rd!"), Salt(), 0));
    }
}

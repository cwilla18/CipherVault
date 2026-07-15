using System;
using System.Security;
using System.Text;
using CipherVault.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Core;

[TestClass]
public class PasswordHelperTests
{
    private static SecureString Secure(string value)
    {
        var secure = new SecureString();
        foreach (var c in value)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }

    [TestMethod]
    public void UsePasswordChars_ExposesExactCharacters()
    {
        var password = "Password123!";
        using var secure = Secure(password);

        var seen = PasswordHelper.UsePasswordChars(secure, chars => new string(chars));

        Assert.AreEqual(password, seen);
    }

    [TestMethod]
    public void UsePasswordChars_ZeroesBufferAfterUse()
    {
        using var secure = Secure("Password123!");

        char[] captured = null!;
        PasswordHelper.UsePasswordChars(secure, chars =>
        {
            captured = chars; // capture the same array the helper will clear
            return 0;
        });

        Assert.IsTrue(Array.TrueForAll(captured, c => c == '\0'), "the char buffer should be zeroed once the callback returns");
    }

    [TestMethod]
    public void UsePasswordBytes_ExposesUtf8Bytes()
    {
        var password = "Password123!";
        using var secure = Secure(password);

        var matches = PasswordHelper.UsePasswordBytes(secure, bytes => bytes.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(password)));

        Assert.IsTrue(matches);
    }

    [TestMethod]
    public void UsePasswordChars_NullSecureString_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => PasswordHelper.UsePasswordChars<int>(null!, _ => 0));
    }
}

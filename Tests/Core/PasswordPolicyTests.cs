using CipherVault.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Core;

[TestClass]
public class PasswordPolicyTests
{
    [DataTestMethod]
    [DataRow("Abcdef1!gh")]      // exactly 10, all four character classes
    [DataRow("Abcde123!X")]      // another 10-char boundary case
    [DataRow("MyStr0ng!Password123")] // comfortably long
    public void IsValid_MeetsAllRules_ReturnsTrue(string password)
    {
        Assert.IsTrue(PasswordPolicy.IsValid(password));
    }

    [DataTestMethod]
    [DataRow("Abcde1!fg")]   // 9 chars – too short
    [DataRow("Abc1!")]       // way too short
    [DataRow("abcdefg1h!")]  // no uppercase
    [DataRow("ABCDEFG1H!")]  // no lowercase
    [DataRow("Abcdefgh!i")]  // no digit
    [DataRow("Abcdefgh1i")]  // no special character
    public void IsValid_MissingARequirement_ReturnsFalse(string password)
    {
        Assert.IsFalse(PasswordPolicy.IsValid(password));
    }

    [TestMethod]
    public void IsValid_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(PasswordPolicy.IsValid(string.Empty));
    }

    [TestMethod]
    public void IsValid_Null_ReturnsFalse()
    {
        Assert.IsFalse(PasswordPolicy.IsValid(null!));
    }
}

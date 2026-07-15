using System;

namespace CipherVault.Core.Helpers;

public static class PasswordPolicy
{
    public static bool IsValid(string? password) => password is not null && IsValid(password.AsSpan());

    public static bool IsValid(ReadOnlySpan<char> password)
    {
        if (password.Length < 10)
        {
            return false;
        }

        var hasUpperCase = false;
        var hasLowerCase = false;
        var hasDigit = false;
        var hasSpecialChar = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c))
            {
                hasUpperCase = true;
            }
            else if (char.IsLower(c))
            {
                hasLowerCase = true;
            }
            else if (char.IsDigit(c))
            {
                hasDigit = true;
            }
            else if (!char.IsLetterOrDigit(c))
            {
                hasSpecialChar = true;
            }

            // If all conditions are met, we can exit early
            if (hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar)
            {
                return true;
            }
        }

        return false;
    }
}

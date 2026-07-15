using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace CipherVault.Core.Helpers;

public static class PasswordHelper
{
    /// <summary>
    /// Exposes the password as a transient <c>char[]</c> to <paramref name="action"/>
    /// and zeroes it (and the unmanaged copy) before returning. The password is
    /// never materialised as an immutable managed <see cref="string"/>, which
    /// could not be wiped and would linger on the GC heap.
    /// </summary>
    public static T UsePasswordChars<T>(SecureString secureString, Func<char[], T> action)
    {
        ArgumentNullException.ThrowIfNull(secureString);
        ArgumentNullException.ThrowIfNull(action);

        var bstr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
        char[]? chars = null;
        try
        {
            chars = new char[secureString.Length];
            Marshal.Copy(bstr, chars, 0, secureString.Length);
            return action(chars);
        }
        finally
        {
            if (chars is not null)
            {
                Array.Clear(chars);
            }
            Marshal.ZeroFreeGlobalAllocUnicode(bstr);
        }
    }

    /// <summary>
    /// Exposes the password as transient UTF-8 <c>byte[]</c> to <paramref name="action"/>
    /// (for key derivation) and zeroes it before returning.
    /// </summary>
    public static T UsePasswordBytes<T>(SecureString secureString, Func<byte[], T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return UsePasswordChars(secureString, chars =>
        {
            byte[]? bytes = null;
            try
            {
                bytes = Encoding.UTF8.GetBytes(chars);
                return action(bytes);
            }
            finally
            {
                if (bytes is not null)
                {
                    Array.Clear(bytes);
                }
            }
        });
    }
}

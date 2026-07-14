using CipherVault.Core.Data;
using System;

namespace CipherVault.Core.Records;

public record WhileLoopSanityCheck()
{
    public int attempts { get; set; }

    public void ValidateAttempts()
    {
        if (attempts >= Config.WhileLoopSanityCheck)
        {
            throw new InvalidOperationException("Too many invalid attempts.");
        }
        else
        {
            attempts++;
        }
    }
}

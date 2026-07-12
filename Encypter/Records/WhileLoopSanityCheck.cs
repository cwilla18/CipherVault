using Encypter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encypter.Records
{
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
}

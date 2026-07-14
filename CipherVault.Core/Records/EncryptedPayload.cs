using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CipherVault.Core.Records;

public sealed record EncryptedPayload(byte[] Nonce, byte[] Ciphertext, byte[] Tag);

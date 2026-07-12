using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encypter.Records;
internal sealed record EncryptedPayload(byte[] Plaintext, byte[] AssociatedData, byte[] Key);

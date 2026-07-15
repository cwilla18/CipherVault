# CipherVault

A small .NET 8 command-line tool for encrypting a folder of files into a
password-protected vault, and restoring them again later.

Each file is encrypted individually with **AES-256-GCM**, and the resulting
files are then packaged into a single **plain `.zip`** container for
portability. All confidentiality and integrity come from the per-file AES-GCM
layer; the zip itself is purely structural and holds no password. Encryption
and decryption are shipped as two separate console applications that share a
common core library.

> ⚠️ **Your password cannot be recovered.** There is no backdoor and no reset.
> If you lose the password, the encrypted data is gone for good. Keep a copy
> somewhere safe before you encrypt anything important.

---

## Contents

- [How it works](#how-it-works)
- [The `.cwe` file format](#the-cwe-file-format)
- [Password policy](#password-policy)
- [Getting started](#getting-started)
- [Usage](#usage)
- [Project layout](#project-layout)
- [Running the tests](#running-the-tests)
- [Security notes](#security-notes)
- [Known limitations](#known-limitations)
- [Contributing](#contributing)

---

## How it works

### Encryption (`CipherVault`)

1. You provide the path to a folder you want to protect.
2. You set a password (entered twice, masked, and checked against the
   [password policy](#password-policy)).
3. A random 16-byte salt is generated, and an AES key is derived from your
   password using **PBKDF2 (SHA-256, 700,000 iterations)**.
4. The folder's contents are copied into a working `<folder>_encrypted`
   directory, **preserving the original subfolder structure**.
5. Each file is encrypted with **AES-256-GCM** (a fresh random nonce per file)
   and written out as a `.cwe` file (see the [format](#the-cwe-file-format)).
   The header (version, iterations, salt) is bound to the authentication tag as
   associated data.
6. Each `.cwe` is **read back and decrypted to verify it is recoverable before
   the corresponding plaintext is deleted** — a failed verification leaves the
   original in place.
7. The working directory is packaged into a single plain `.zip`, and the
   intermediate directory is removed only after the archive is confirmed on disk.

### Decryption (`Decrypter`)

1. You provide the path to the encrypted `.zip` and the password.
2. The archive is extracted.
3. Each `.cwe` file is parsed; the key is re-derived from the password using
   the salt and iteration count stored in the file header.
4. The AES-GCM authentication tag is verified (decryption fails cleanly if the
   password is wrong or the data has been tampered with), and the original file
   is restored.

---

## The `.cwe` file format

Every encrypted file is a binary `.cwe` file with a fixed-size header followed
by the ciphertext. All multi-byte integers are little-endian.

| Field        | Size (bytes) | Notes                                         |
|--------------|--------------|-----------------------------------------------|
| Magic        | 4            | ASCII `CWEF`                                  |
| Version      | 1            | Currently `1`                                 |
| Iterations   | 4            | PBKDF2 iteration count (Int32)                |
| Salt         | 16           | PBKDF2 salt                                   |
| Nonce        | 12           | AES-GCM nonce (unique per file)               |
| Tag          | 16           | AES-GCM authentication tag                    |
| Ciphertext   | remainder    | Encrypted file contents                       |

Storing the iteration count and salt in each file means files stay decryptable
even if the tool's default parameters change in a later version.

The magic, version, iterations and salt are also passed to AES-GCM as
**associated data**, so tampering with any header field (or swapping ciphertext
between files) causes authentication to fail on decrypt.

The size constants live in [`CipherVault.Core/Data/Config.cs`](CipherVault.Core/Data/Config.cs).

---

## Password policy

A password is accepted only if it:

- is at least **10 characters** long, and
- contains at least one **uppercase** letter, one **lowercase** letter, one
  **digit**, and one **special character**.

The policy is implemented in
[`CipherVault.Core/Helpers/PasswordPolicy.cs`](CipherVault.Core/Helpers/PasswordPolicy.cs).

---

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later

### Build

```bash
dotnet build CipherVault.sln
```

### Run

```bash
# Encrypt a folder
dotnet run --project CipherVault

# Decrypt a vault
dotnet run --project Decrypter
```

Both apps are interactive and will prompt you for the paths and password.

---

## Usage

### Encrypting a folder

```
$ dotnet run --project CipherVault

================================================================
  CipherVault  -  Encrypt
  Securely encrypt a folder into a password-protected vault.
================================================================

-- Select folder ----------------------------------------------
  Enter the path to the folder you want to encrypt:
  > C:\Users\me\Documents\Secrets

-- Set password -----------------------------------------------
  Password must be at least 10 characters and include an uppercase letter,
  a lowercase letter, a digit and a special character.
  Enter a password to encrypt the files:
  > **********
  Confirm the password:
  > **********

-- Encrypting -------------------------------------------------
  [3/3] report.pdf

+--------------------------------------------------------------+
| Encryption complete                                          |
+--------------------------------------------------------------+
| Files encrypted   3                                          |
| Output            ...\Secrets_encrypted.zip                  |
| Elapsed           0.4s                                       |
+--------------------------------------------------------------+
  [OK]   Your files are encrypted. Keep your password safe - it cannot be recovered.
```

### Decrypting a vault

```
$ dotnet run --project Decrypter

  Enter the path to the encrypted .zip file:
  > C:\Users\me\Documents\Secrets_encrypted.zip
  Enter the password used to encrypt the file:
  > **********
```

The decrypted files are written to a `<name>_decrypted` folder next to the
archive.

---

## Project layout

| Project                | Purpose                                                        |
|------------------------|----------------------------------------------------------------|
| `CipherVault`          | Encryption console app (entry point + `EncryptionService`).    |
| `Decrypter`            | Decryption console app (entry point + `DecryptionService`).    |
| `CipherVault.Core`     | Shared library: cipher, `.cwe` format, helpers, console UI.    |
| `Tests`                | MSTest unit and round-trip tests.                              |

Key types in `CipherVault.Core`:

- `AesGcmCipher` — AES-256-GCM encrypt/decrypt and PBKDF2 key derivation
  (`ICipher`).
- `CweFileFormat` — reads and writes the `.cwe` binary format.
- `ConsoleUi` — dependency-free, coloured console UI: banners, prompts, masked
  secret entry, progress and summary rendering (`IConsoleUi`).
- `PasswordPolicy` / `PasswordHelper` — policy checks and `SecureString`
  handling.

---

## Running the tests

```bash
dotnet test
```

The `Tests` project covers the cipher, the `.cwe` format, the password policy
and helpers, the console UI, and full encrypt → decrypt round trips.

---

## Security notes

- **AES-256-GCM** provides confidentiality and integrity: decryption fails if
  the ciphertext or header has been altered, or if the password is wrong. The
  header is bound to the tag as associated data.
- A **fresh random nonce** is generated for every file.
- Keys are derived with **PBKDF2-SHA256 (700,000 iterations)**; the salt and
  iteration count are stored per file so parameters can be strengthened over time.
- Passwords are held in a `SecureString` and converted only to a transient,
  zeroed `char[]`/`byte[]` for policy checking and key derivation — never to an
  immutable managed `string`.
- Encryption **verifies each `.cwe` decrypts back to the original bytes before
  deleting any plaintext**, so a corrupt write cannot lose data.

---

## Known limitations

These are current behaviours worth being aware of. Contributions welcome.

- **Plaintext originals at the source root are not removed.** Encryption works
  on a copy; the source folder's original files remain in place alongside the
  produced `.zip`. Remove them yourself if the originals should not persist.
- **Whole files are read into memory.** Encryption and decryption are
  synchronous and buffer each file fully, so very large files are constrained by
  available memory.
- The ZIP layer uses [DotNetZip](https://www.nuget.org/packages/DotNetZip),
  which is no longer maintained and has a known
  [high-severity advisory](https://github.com/advisories/GHSA-xhg6-9j5j-w4vf).
  Now that the archive is a plain container, this could be replaced by the
  built-in `System.IO.Compression` with no third-party dependency.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

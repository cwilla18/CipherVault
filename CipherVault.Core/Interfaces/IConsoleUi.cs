using System.Collections.Generic;
using System.Security;

namespace CipherVault.Core.Interfaces;

/// <summary>
/// User-facing console presentation. This is deliberately separate from
/// <see cref="Microsoft.Extensions.Logging.ILogger"/>: the logger is for
/// diagnostics, this is for the person sitting in front of the tool.
/// </summary>
public interface IConsoleUi
{
    /// <summary>Prints a framed title header with an optional subtitle.</summary>
    void Banner(string title, string? subtitle = null);

    /// <summary>Prints a horizontal rule with a section label.</summary>
    void Section(string title);

    void Info(string message);
    void Success(string message);
    void Warn(string message);
    void Error(string message);

    /// <summary>Reads a line of free text.</summary>
    string Prompt(string message);

    /// <summary>Asks a yes/no question. Enter accepts the default.</summary>
    bool Confirm(string message, bool defaultYes = true);

    /// <summary>Prompts until the user supplies a path to an existing directory.</summary>
    string PromptExistingDirectory(string message);

    /// <summary>
    /// Prompts until the user supplies a path to an existing file, optionally
    /// requiring a specific extension (e.g. ".zip").
    /// </summary>
    string PromptExistingFile(string message, string? requiredExtension = null);

    /// <summary>Reads a masked secret (password) without echoing characters.</summary>
    SecureString ReadSecret(string message);

    /// <summary>
    /// Reads a masked secret twice and only returns once both entries match.
    /// Use on encryption, where a typo would make data unrecoverable.
    /// </summary>
    SecureString ReadSecretConfirmed(string message, string confirmMessage);

    /// <summary>Renders an in-place "[current/total] label" progress line.</summary>
    void Progress(int current, int total, string label);

    /// <summary>Prints a boxed key/value summary block.</summary>
    void Summary(string title, IReadOnlyList<(string Label, string Value)> rows);
}

namespace SarifHub.Domain.Common;

/// <summary>
/// A domain rule was violated. The message is written for a developer, not an end user:
/// requests are validated before they reach the domain, so reaching this is a bug or bad data.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Creates the exception.</summary>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with no message.</summary>
    public DomainException()
    {
    }

    /// <summary>Creates the exception wrapping another one.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

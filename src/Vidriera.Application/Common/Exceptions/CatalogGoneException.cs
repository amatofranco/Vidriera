namespace Vidriera.Application.Common.Exceptions;

/// <summary>Thrown when a generated catalog exists but is expired or revoked (maps to HTTP 410).</summary>
public class CatalogGoneException : Exception
{
    public CatalogGoneException(string message) : base(message)
    {
    }
}

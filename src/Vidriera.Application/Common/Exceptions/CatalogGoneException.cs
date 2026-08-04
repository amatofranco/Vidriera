namespace Vidriera.Application.Common.Exceptions;

public class CatalogGoneException : Exception
{
    public CatalogGoneException(string message) : base(message)
    {
    }
}

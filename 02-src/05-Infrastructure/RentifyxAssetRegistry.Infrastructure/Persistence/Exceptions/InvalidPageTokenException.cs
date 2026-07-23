namespace RentifyxAssetRegistry.Infrastructure.Persistence.Exceptions;

/// <summary>
/// Thrown when an opaque cursor pagination token cannot be decoded back into a DynamoDB
/// <c>ExclusiveStartKey</c> (malformed/tampered token). Infrastructure never returns
/// <c>ErrorOr</c>, so this is a typed exception the Application layer is taught to translate into
/// a validation error (DYN-11).
/// </summary>
public sealed class InvalidPageTokenException : Exception
{
    public InvalidPageTokenException()
        : base("The provided page token is invalid.")
    {
    }

    public InvalidPageTokenException(string message)
        : base(message)
    {
    }

    public InvalidPageTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

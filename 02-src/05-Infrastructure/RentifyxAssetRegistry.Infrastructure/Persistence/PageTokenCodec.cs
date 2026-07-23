using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Infrastructure.Persistence.Exceptions;

namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// Encodes/decodes an opaque cursor pagination token to/from a DynamoDB
/// <c>ExclusiveStartKey</c>/<c>LastEvaluatedKey</c> dictionary. All search-relevant keys in this
/// schema are string (S) attribute values, so the dictionary is round-tripped through
/// <c>Dictionary&lt;string, string&gt;</c> instead of hand-rolling an <see cref="AttributeValue"/>
/// JSON converter.
/// </summary>
public static class PageTokenCodec
{
    public static string Encode(Dictionary<string, AttributeValue> lastEvaluatedKey)
    {
        Dictionary<string, string> flattened = lastEvaluatedKey.ToDictionary(pair => pair.Key, pair => pair.Value.S);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(flattened);
        return Convert.ToBase64String(bytes);
    }

    public static Dictionary<string, AttributeValue> Decode(string token)
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(token);
            Dictionary<string, string>? flattened = JsonSerializer.Deserialize<Dictionary<string, string>>(bytes);

            if (flattened is null || flattened.Count == 0)
                throw new InvalidPageTokenException();

            return flattened.ToDictionary(pair => pair.Key, pair => new AttributeValue(pair.Value));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new InvalidPageTokenException("The provided page token could not be decoded.", ex);
        }
    }
}

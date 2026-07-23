namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// PK/SK/GSI attribute names and key-prefix constants for the single-table design described in
/// .specs/features/e04-f10-dynamodb-repository/design.md.
/// </summary>
public static class DynamoDbKeys
{
    // Attribute names (shared across item types).
    public const string Pk = "PK";
    public const string Sk = "SK";
    public const string Type = "Type";
    public const string Gsi1Pk = "GSI1PK";
    public const string Gsi1Sk = "GSI1SK";
    public const string Gsi2Pk = "GSI2PK";
    public const string Gsi2Sk = "GSI2SK";
    public const string Gsi3Pk = "GSI3PK";
    public const string Gsi3Sk = "GSI3SK";
    public const string Gsi4Pk = "GSI4PK";
    public const string Gsi4Sk = "GSI4SK";

    // Index names.
    public const string Gsi1IndexName = "GSI1";
    public const string Gsi2IndexName = "GSI2";
    public const string Gsi3IndexName = "GSI3";
    public const string Gsi4IndexName = "GSI4";

    // Item type discriminators.
    public const string AssetType = "Asset";
    public const string CategoryType = "Category";
    public const string OutboxType = "Outbox";

    // Key prefixes.
    public const string AssetPrefix = "ASSET#";
    public const string CategoryPrefix = "CATEGORY#";
    public const string OwnerPrefix = "OWNER#";
    public const string IdempotencyPrefix = "IDEMPOTENCY#";
    public const string StatusPrefix = "STATUS#";
    public const string OutboxPrefix = "OUTBOX#";
    public const string OutboxStatusPrefix = "OUTBOX_STATUS#";
    public const string CategoryListValue = "CATEGORY_LIST";

    public static string AssetKey(Guid id) => $"{AssetPrefix}{id}";

    public static string AssetSortKey(DateTime createdAt, Guid id) => $"{AssetPrefix}{createdAt:o}#{id}";

    public static string OwnerKey(Guid ownerId) => $"{OwnerPrefix}{ownerId}";

    public static string CategoryPartitionKey(Guid categoryId) => $"{CategoryPrefix}{categoryId}";

    public static string IdempotencyKey(string idempotencyKey) => $"{IdempotencyPrefix}{idempotencyKey}";

    public static string StatusKey(string status) => $"{StatusPrefix}{status}";

    public static string CategoryKey(Guid id) => $"{CategoryPrefix}{id}";

    public static string CategorySortKey(int depth, string name, Guid id) =>
        $"{CategoryPrefix}{depth:D2}#{name}#{id}";

    public static string OutboxKey(Guid id) => $"{OutboxPrefix}{id}";

    public static string OutboxStatusKey(string status) => $"{OutboxStatusPrefix}{status}";

    public static string OutboxSortKey(DateTime createdAtUtc, Guid id) => $"{createdAtUtc:o}#{id}";
}

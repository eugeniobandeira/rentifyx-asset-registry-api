using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using Testcontainers.LocalStack;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories.Fixtures;

/// <summary>
/// Spins up a real LocalStack DynamoDB container once per test collection and creates the
/// single-table schema (table + 4 GSIs) described in
/// .specs/features/e04-f10-dynamodb-repository/design.md.
/// </summary>
public sealed class LocalStackFixture : IAsyncLifetime
{
    private const string TestTableName = "AssetRegistryTest";

    private LocalStackContainer _container = null!;

    public IAmazonDynamoDB Client { get; private set; } = null!;

    public IDynamoDBContext Context { get; private set; } = null!;

    public DynamoDbOptions Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // "latest" (LocalStack 4.15+) requires a LOCALSTACK_AUTH_TOKEN; pin to the last
        // token-free community-edition tag instead.
        _container = new LocalStackBuilder("localstack/localstack:3.8")
            .Build();

        await _container.StartAsync();

        Options = new DynamoDbOptions
        {
            TableName = TestTableName,
            Region = "us-east-1",
            ServiceUrl = _container.GetConnectionString()
        };

        AmazonDynamoDBConfig clientConfig = new()
        {
            ServiceURL = Options.ServiceUrl,
            AuthenticationRegion = Options.Region,
            UseHttp = true
        };

        // LocalStack accepts any credentials, but the AWS SDK validates access-key-id shape
        // client-side, so a well-formed (if fake) key pair is used rather than "test"/"test".
        Client = new AmazonDynamoDBClient(
            new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"),
            clientConfig);
        Context = new DynamoDBContextBuilder().WithDynamoDBClient(() => Client).Build();

        await CreateSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    private async Task CreateSchemaAsync()
    {
        await Client.CreateTableAsync(new CreateTableRequest
        {
            TableName = TestTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new AttributeDefinition(DynamoDbKeys.Pk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Sk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi1Pk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi1Sk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi2Pk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi2Sk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi3Pk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi3Sk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi4Pk, ScalarAttributeType.S),
                new AttributeDefinition(DynamoDbKeys.Gsi4Sk, ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement(DynamoDbKeys.Pk, KeyType.HASH),
                new KeySchemaElement(DynamoDbKeys.Sk, KeyType.RANGE)
            ],
            GlobalSecondaryIndexes =
            [
                BuildGsi(DynamoDbKeys.Gsi1IndexName, DynamoDbKeys.Gsi1Pk, DynamoDbKeys.Gsi1Sk),
                BuildGsi(DynamoDbKeys.Gsi2IndexName, DynamoDbKeys.Gsi2Pk, DynamoDbKeys.Gsi2Sk),
                BuildGsi(DynamoDbKeys.Gsi3IndexName, DynamoDbKeys.Gsi3Pk, DynamoDbKeys.Gsi3Sk),
                BuildGsi(DynamoDbKeys.Gsi4IndexName, DynamoDbKeys.Gsi4Pk, DynamoDbKeys.Gsi4Sk)
            ]
        });

        await WaitUntilActiveAsync();
    }

    private static GlobalSecondaryIndex BuildGsi(string indexName, string pkAttribute, string skAttribute) => new()
    {
        IndexName = indexName,
        KeySchema =
        [
            new KeySchemaElement(pkAttribute, KeyType.HASH),
            new KeySchemaElement(skAttribute, KeyType.RANGE)
        ],
        Projection = new Projection { ProjectionType = ProjectionType.ALL }
    };

    private async Task WaitUntilActiveAsync()
    {
        while (true)
        {
            DescribeTableResponse response = await Client.DescribeTableAsync(TestTableName);

            if (response.Table.TableStatus == TableStatus.ACTIVE)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class LocalStackFixtureGroup : ICollectionFixture<LocalStackFixture>
{
    public const string Name = "LocalStack";
}

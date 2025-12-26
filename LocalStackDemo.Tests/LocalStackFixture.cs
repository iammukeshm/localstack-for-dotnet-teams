using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;

namespace LocalStackDemo.Tests;

public class LocalStackFixture : IDisposable
{
    public IAmazonS3 S3Client { get; }
    public IAmazonDynamoDB DynamoDbClient { get; }
    public IAmazonSQS SqsClient { get; }

    public string BucketName { get; }
    public string TableName { get; }
    public string QueueName { get; }

    public LocalStackFixture()
    {
        var serviceUrl = Environment.GetEnvironmentVariable("AWS__ServiceUrl") ?? "http://localhost:4566";
        var region = RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS__Region") ?? "us-east-1");

        BucketName = Environment.GetEnvironmentVariable("Resources__BucketName") ?? "orders-receipts";
        TableName = Environment.GetEnvironmentVariable("Resources__TableName") ?? "Orders";
        QueueName = Environment.GetEnvironmentVariable("Resources__QueueName") ?? "orders-events";

        // Use dummy credentials for LocalStack (it doesn't validate them)
        var credentials = new BasicAWSCredentials("test", "test");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = region.SystemName
        };
        S3Client = new AmazonS3Client(credentials, s3Config);

        var dynamoConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = serviceUrl,
            AuthenticationRegion = region.SystemName
        };
        DynamoDbClient = new AmazonDynamoDBClient(credentials, dynamoConfig);

        var sqsConfig = new AmazonSQSConfig
        {
            ServiceURL = serviceUrl,
            AuthenticationRegion = region.SystemName
        };
        SqsClient = new AmazonSQSClient(credentials, sqsConfig);
    }

    public void Dispose()
    {
        S3Client.Dispose();
        DynamoDbClient.Dispose();
        SqsClient.Dispose();
    }
}

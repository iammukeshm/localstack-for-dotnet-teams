using Amazon;
using Amazon.DynamoDBv2;
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
        var region = Environment.GetEnvironmentVariable("AWS__Region") ?? "us-east-1";

        BucketName = Environment.GetEnvironmentVariable("Resources__BucketName") ?? "orders-receipts";
        TableName = Environment.GetEnvironmentVariable("Resources__TableName") ?? "Orders";
        QueueName = Environment.GetEnvironmentVariable("Resources__QueueName") ?? "orders-events";

        var s3Config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            UseHttp = true,
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };
        S3Client = new AmazonS3Client(s3Config);

        var dynamoConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = serviceUrl,
            UseHttp = true,
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };
        DynamoDbClient = new AmazonDynamoDBClient(dynamoConfig);

        var sqsConfig = new AmazonSQSConfig
        {
            ServiceURL = serviceUrl,
            UseHttp = true,
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };
        SqsClient = new AmazonSQSClient(sqsConfig);
    }

    public void Dispose()
    {
        S3Client.Dispose();
        DynamoDbClient.Dispose();
        SqsClient.Dispose();
    }
}

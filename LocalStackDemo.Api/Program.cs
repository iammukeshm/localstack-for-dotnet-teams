using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using LocalStackDemo.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var awsSection = builder.Configuration.GetSection("AWS");
var region = awsSection["Region"] ?? "us-east-1";
var useLocalStack = awsSection.GetValue<bool>("UseLocalStack");
var serviceUrl = awsSection["ServiceUrl"];

var resourcesSection = builder.Configuration.GetSection("Resources");
var bucketName = resourcesSection["BucketName"] ?? "orders-receipts";
var tableName = resourcesSection["TableName"] ?? "Orders";
var queueName = resourcesSection["QueueName"] ?? "orders-events";

// Register S3 client
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(region)
    };

    if (useLocalStack && !string.IsNullOrEmpty(serviceUrl))
    {
        config.ServiceURL = serviceUrl;
        config.ForcePathStyle = true; // Required for LocalStack S3
        config.UseHttp = true;
    }

    return new AmazonS3Client(config);
});

// Register DynamoDB client
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(region)
    };

    if (useLocalStack && !string.IsNullOrEmpty(serviceUrl))
    {
        config.ServiceURL = serviceUrl;
        config.UseHttp = true;
    }

    return new AmazonDynamoDBClient(config);
});

// Register SQS client
builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var config = new AmazonSQSConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(region)
    };

    if (useLocalStack && !string.IsNullOrEmpty(serviceUrl))
    {
        config.ServiceURL = serviceUrl;
        config.UseHttp = true;
    }

    return new AmazonSQSClient(config);
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// Health check
app.MapGet("/", () => Results.Ok(new
{
    Status = "Running",
    Mode = useLocalStack ? "LocalStack" : "AWS",
    Timestamp = DateTime.UtcNow
}));

// Create order endpoint
app.MapPost("/orders", async (
    CreateOrderRequest request,
    IAmazonDynamoDB dynamoDb,
    IAmazonS3 s3,
    IAmazonSQS sqs) =>
{
    var order = new Order(
        OrderId: Guid.NewGuid().ToString(),
        CustomerEmail: request.CustomerEmail,
        Amount: request.Amount,
        CreatedAt: DateTime.UtcNow
    );

    // 1. Save to DynamoDB
    var putRequest = new PutItemRequest
    {
        TableName = tableName,
        Item = new Dictionary<string, AttributeValue>
        {
            ["OrderId"] = new(order.OrderId),
            ["CustomerEmail"] = new(order.CustomerEmail),
            ["Amount"] = new() { N = order.Amount.ToString() },
            ["CreatedAt"] = new(order.CreatedAt.ToString("O"))
        }
    };
    await dynamoDb.PutItemAsync(putRequest);

    // 2. Upload receipt to S3
    var receipt = $"""
        Order Receipt

        Order ID: {order.OrderId}
        Customer: {order.CustomerEmail}
        Amount: ${order.Amount:F2}
        Date: {order.CreatedAt:F}
        """;
    var putObjectRequest = new PutObjectRequest
    {
        BucketName = bucketName,
        Key = $"receipts/{order.OrderId}.txt",
        ContentBody = receipt
    };
    await s3.PutObjectAsync(putObjectRequest);

    // 3. Send message to SQS
    var queueUrlResponse = await sqs.GetQueueUrlAsync(queueName);
    var sendMessageRequest = new SendMessageRequest
    {
        QueueUrl = queueUrlResponse.QueueUrl,
        MessageBody = JsonSerializer.Serialize(order)
    };
    await sqs.SendMessageAsync(sendMessageRequest);

    return Results.Created($"/orders/{order.OrderId}", order);
});

// Get order by ID
app.MapGet("/orders/{orderId}", async (string orderId, IAmazonDynamoDB dynamoDb) =>
{
    var response = await dynamoDb.GetItemAsync(new GetItemRequest
    {
        TableName = tableName,
        Key = new Dictionary<string, AttributeValue>
        {
            ["OrderId"] = new(orderId)
        }
    });

    if (response.Item.Count == 0)
        return Results.NotFound(new { Message = "Order not found" });

    return Results.Ok(new Order(
        OrderId: response.Item["OrderId"].S,
        CustomerEmail: response.Item["CustomerEmail"].S,
        Amount: decimal.Parse(response.Item["Amount"].N),
        CreatedAt: DateTime.Parse(response.Item["CreatedAt"].S)
    ));
});

// List all orders
app.MapGet("/orders", async (IAmazonDynamoDB dynamoDb) =>
{
    var response = await dynamoDb.ScanAsync(new ScanRequest { TableName = tableName });

    var orders = response.Items.Select(item => new Order(
        OrderId: item["OrderId"].S,
        CustomerEmail: item["CustomerEmail"].S,
        Amount: decimal.Parse(item["Amount"].N),
        CreatedAt: DateTime.Parse(item["CreatedAt"].S)
    ));

    return Results.Ok(orders);
});

// Check SQS messages (for debugging)
app.MapGet("/messages", async (IAmazonSQS sqs) =>
{
    var queueUrlResponse = await sqs.GetQueueUrlAsync(queueName);
    var receiveResponse = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
    {
        QueueUrl = queueUrlResponse.QueueUrl,
        MaxNumberOfMessages = 10,
        WaitTimeSeconds = 1
    });

    return Results.Ok(receiveResponse.Messages.Select(m => new
    {
        m.MessageId,
        m.Body,
        m.ReceiptHandle
    }));
});

// List S3 receipts
app.MapGet("/receipts", async (IAmazonS3 s3) =>
{
    var response = await s3.ListObjectsV2Async(new ListObjectsV2Request
    {
        BucketName = bucketName,
        Prefix = "receipts/"
    });

    return Results.Ok(response.S3Objects.Select(o => new
    {
        o.Key,
        o.Size,
        o.LastModified
    }));
});

// Get receipt content
app.MapGet("/receipts/{orderId}", async (string orderId, IAmazonS3 s3) =>
{
    try
    {
        var response = await s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = $"receipts/{orderId}.txt"
        });

        using var reader = new StreamReader(response.ResponseStream);
        var content = await reader.ReadToEndAsync();

        return Results.Ok(new { OrderId = orderId, Content = content });
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { Message = "Receipt not found" });
    }
});

app.Run();

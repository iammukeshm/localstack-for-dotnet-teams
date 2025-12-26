using Amazon.DynamoDBv2.Model;
using Amazon.S3.Model;
using Xunit;

namespace LocalStackDemo.Tests;

public class IntegrationTests : IClassFixture<LocalStackFixture>
{
    private readonly LocalStackFixture _fixture;

    public IntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DynamoDB_CanWriteAndReadOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid().ToString();
        var customerEmail = "test@example.com";
        var amount = 99.99m;

        // Act - Write
        var putRequest = new PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["OrderId"] = new(orderId),
                ["CustomerEmail"] = new(customerEmail),
                ["Amount"] = new() { N = amount.ToString() },
                ["CreatedAt"] = new(DateTime.UtcNow.ToString("O"))
            }
        };
        await _fixture.DynamoDbClient.PutItemAsync(putRequest);

        // Act - Read
        var getRequest = new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["OrderId"] = new(orderId)
            }
        };
        var response = await _fixture.DynamoDbClient.GetItemAsync(getRequest);

        // Assert
        Assert.NotNull(response.Item);
        Assert.Equal(orderId, response.Item["OrderId"].S);
        Assert.Equal(customerEmail, response.Item["CustomerEmail"].S);
        Assert.Equal(amount.ToString(), response.Item["Amount"].N);
    }

    [Fact]
    public async Task S3_CanUploadAndDownloadReceipt()
    {
        // Arrange
        var orderId = Guid.NewGuid().ToString();
        var receiptContent = $"Order Receipt for {orderId}";
        var key = $"receipts/{orderId}.txt";

        // Act - Upload
        var putRequest = new PutObjectRequest
        {
            BucketName = _fixture.BucketName,
            Key = key,
            ContentBody = receiptContent
        };
        await _fixture.S3Client.PutObjectAsync(putRequest);

        // Act - Download
        var getRequest = new GetObjectRequest
        {
            BucketName = _fixture.BucketName,
            Key = key
        };
        var response = await _fixture.S3Client.GetObjectAsync(getRequest);

        using var reader = new StreamReader(response.ResponseStream);
        var downloadedContent = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(receiptContent, downloadedContent);
    }
}

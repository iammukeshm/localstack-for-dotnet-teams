# LocalStack for .NET Developers

This repository demonstrates how to use **LocalStack** to run AWS services locally for .NET development. Build and test your AWS-integrated applications without incurring cloud costs.

**Article**: [LocalStack for .NET Developers – Run AWS Services Locally for Free](https://codewithmukesh.com/blog/localstack-for-dotnet-teams/)

## What's Included

A .NET 10 Minimal API that demonstrates:

- **DynamoDB** – Store and retrieve order records
- **S3** – Upload and list order receipt files
- **SQS** – Publish and receive order event messages

All services run locally via LocalStack, with configuration-based switching to real AWS.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)

## Quick Start

### 1. Start LocalStack and Create Resources

**Windows (PowerShell):**
```powershell
.\setup-localstack.ps1
```

**Linux/macOS:**
```bash
chmod +x setup-localstack.sh
./setup-localstack.sh
```

**Or manually:**
```bash
# Set dummy credentials (LocalStack doesn't validate them)
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Start LocalStack
docker compose up -d

# Wait for it to be ready, then create resources
aws --endpoint-url=http://localhost:4566 s3 mb s3://orders-receipts
aws --endpoint-url=http://localhost:4566 dynamodb create-table --table-name Orders --attribute-definitions AttributeName=OrderId,AttributeType=S --key-schema AttributeName=OrderId,KeyType=HASH --billing-mode PAY_PER_REQUEST
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name orders-events
```

### 2. Run the .NET Application

```bash
dotnet run --project LocalStackDemo.Api
```

The API will start at `http://localhost:5000` (or the port shown in the console).

### 3. Test the API

Open the Scalar API documentation at `http://localhost:5000/scalar/v1`.

**Create an order:**
```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerEmail": "test@example.com", "amount": 99.99}'
```

**List orders:**
```bash
curl http://localhost:5000/orders
```

**Check SQS messages:**
```bash
curl http://localhost:5000/messages
```

**List S3 receipts:**
```bash
curl http://localhost:5000/receipts
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Health check (shows LocalStack/AWS mode) |
| POST | `/orders` | Create a new order |
| GET | `/orders` | List all orders |
| GET | `/orders/{orderId}` | Get order by ID |
| GET | `/messages` | View SQS messages |
| GET | `/receipts` | List S3 receipt files |
| GET | `/receipts/{orderId}` | Get receipt content |

## Configuration

The application uses configuration-based endpoint switching:

**Development (LocalStack)** - `appsettings.Development.json`:
```json
{
  "AWS": {
    "UseLocalStack": true,
    "ServiceUrl": "http://localhost:4566"
  }
}
```

**Production (Real AWS)** - `appsettings.json`:
```json
{
  "AWS": {
    "UseLocalStack": false
  }
}
```

## Verifying LocalStack Data

```bash
# Check DynamoDB
aws --endpoint-url=http://localhost:4566 dynamodb scan --table-name Orders

# Check S3
aws --endpoint-url=http://localhost:4566 s3 ls s3://orders-receipts/receipts/

# Check SQS
aws --endpoint-url=http://localhost:4566 sqs receive-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/orders-events
```

## Cleanup

```bash
# Stop LocalStack
docker compose down

# Remove persisted data (Windows)
rmdir /s /q localstack-data

# Remove persisted data (Linux/macOS)
rm -rf localstack-data/
```

## Switching to Real AWS

1. Set `AWS:UseLocalStack` to `false`
2. Configure AWS credentials (environment variables, AWS profile, or IAM role)
3. Create the resources in your AWS account
4. Run the application

## License

MIT

## Author

**Mukesh Murugan** - [codewithmukesh.com](https://codewithmukesh.com)

- Twitter: [@iammukeshm](https://twitter.com/iammukeshm)
- GitHub: [@iammukeshm](https://github.com/iammukeshm)

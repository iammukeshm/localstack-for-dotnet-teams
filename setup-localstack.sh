#!/bin/bash
# LocalStack Setup Script for Linux/macOS
# Requires: Docker, AWS CLI

set -e

echo -e "\033[36mSetting up LocalStack for LocalStackDemo...\033[0m"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "\033[31mError: Docker is not running. Please start Docker.\033[0m"
    exit 1
fi

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo -e "\033[31mError: AWS CLI is not installed. Please install it from https://aws.amazon.com/cli/\033[0m"
    exit 1
fi

# Start LocalStack
echo -e "\n\033[33mStarting LocalStack...\033[0m"
docker compose up -d

# Wait for LocalStack to be ready
echo -e "\033[33mWaiting for LocalStack to be ready...\033[0m"
timeout 60 bash -c 'until curl -s http://localhost:4566/_localstack/health | grep -q "running"; do sleep 2; done'
echo -e "\033[32mLocalStack is ready!\033[0m"

# Set environment variables
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Create S3 bucket
echo -e "\n\033[33mCreating S3 bucket: orders-receipts...\033[0m"
aws --endpoint-url=http://localhost:4566 s3 mb s3://orders-receipts || true
echo -e "\033[32mS3 bucket created.\033[0m"

# Create DynamoDB table
echo -e "\033[33mCreating DynamoDB table: Orders...\033[0m"
aws --endpoint-url=http://localhost:4566 dynamodb create-table --table-name Orders --attribute-definitions AttributeName=OrderId,AttributeType=S --key-schema AttributeName=OrderId,KeyType=HASH --billing-mode PAY_PER_REQUEST || true
echo -e "\033[32mDynamoDB table created.\033[0m"

# Create SQS queue
echo -e "\033[33mCreating SQS queue: orders-events...\033[0m"
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name orders-events || true
echo -e "\033[32mSQS queue created.\033[0m"

echo -e "\n\033[36m========================================\033[0m"
echo -e "\033[32mLocalStack setup complete!\033[0m"
echo -e "\033[36m========================================\033[0m"
echo -e "\nResources created:"
echo "  - S3 Bucket: orders-receipts"
echo "  - DynamoDB Table: Orders"
echo "  - SQS Queue: orders-events"
echo -e "\nLocalStack endpoint: http://localhost:4566"
echo -e "\nRun the .NET app with: dotnet run --project LocalStackDemo.Api"
echo ""

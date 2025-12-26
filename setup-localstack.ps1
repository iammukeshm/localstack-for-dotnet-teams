# LocalStack Setup Script for Windows
# Requires: Docker Desktop, AWS CLI

Write-Host "Setting up LocalStack for LocalStackDemo..." -ForegroundColor Cyan

# Check if Docker is running
$dockerStatus = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if AWS CLI is installed
$awsVersion = aws --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: AWS CLI is not installed. Please install it from https://aws.amazon.com/cli/" -ForegroundColor Red
    exit 1
}

# Start LocalStack
Write-Host "`nStarting LocalStack..." -ForegroundColor Yellow
docker compose up -d

# Wait for LocalStack to be ready
Write-Host "Waiting for LocalStack to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
do {
    Start-Sleep -Seconds 2
    $attempt++
    $health = try { Invoke-RestMethod -Uri "http://localhost:4566/_localstack/health" -ErrorAction SilentlyContinue } catch { $null }
} while (-not $health -and $attempt -lt $maxAttempts)

if ($attempt -ge $maxAttempts) {
    Write-Host "Error: LocalStack did not become ready in time." -ForegroundColor Red
    exit 1
}

Write-Host "LocalStack is ready!" -ForegroundColor Green

# Set environment variables for AWS CLI
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
$env:AWS_DEFAULT_REGION = "us-east-1"

# Create S3 bucket
Write-Host "`nCreating S3 bucket: orders-receipts..." -ForegroundColor Yellow
aws --endpoint-url=http://localhost:4566 s3 mb s3://orders-receipts 2>&1 | Out-Null
Write-Host "S3 bucket created." -ForegroundColor Green

# Create DynamoDB table
Write-Host "Creating DynamoDB table: Orders..." -ForegroundColor Yellow
aws --endpoint-url=http://localhost:4566 dynamodb create-table `
    --table-name Orders `
    --attribute-definitions AttributeName=OrderId,AttributeType=S `
    --key-schema AttributeName=OrderId,KeyType=HASH `
    --billing-mode PAY_PER_REQUEST 2>&1 | Out-Null
Write-Host "DynamoDB table created." -ForegroundColor Green

# Create SQS queue
Write-Host "Creating SQS queue: orders-events..." -ForegroundColor Yellow
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name orders-events 2>&1 | Out-Null
Write-Host "SQS queue created." -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "LocalStack setup complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nResources created:"
Write-Host "  - S3 Bucket: orders-receipts"
Write-Host "  - DynamoDB Table: Orders"
Write-Host "  - SQS Queue: orders-events"
Write-Host "`nLocalStack endpoint: http://localhost:4566"
Write-Host "`nRun the .NET app with: dotnet run --project LocalStackDemo.Api"
Write-Host ""

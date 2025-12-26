namespace LocalStackDemo.Api;

public record Order(
    string OrderId,
    string CustomerEmail,
    decimal Amount,
    DateTime CreatedAt
);

public record CreateOrderRequest(
    string CustomerEmail,
    decimal Amount
);

namespace OutboxDemo.DTOs
{
    public record PlaceOrderDto(Guid CustomerId, decimal Total);
}
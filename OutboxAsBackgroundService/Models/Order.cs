namespace OutboxDemo.Models
{
    public class Order
    {
        public long     Id          { get; set; }
        public Guid     CustomerId  { get; set; }
        public decimal  TotalAmount { get; set; }
        public string   Status      { get; set; } = default!;
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    }
}

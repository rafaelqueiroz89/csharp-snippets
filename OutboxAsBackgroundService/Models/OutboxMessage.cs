namespace OutboxDemo.Models
{
    public class OutboxMessage
    {
        public long     Id            { get; set; }
        public string   AggregateType { get; set; } = default!;
        public long     AggregateId   { get; set; }
        public string   Payload       { get; set; } = default!;
        public bool     Sent          { get; set; }
        public DateTime CreatedAt     { get; set; }
    }
}

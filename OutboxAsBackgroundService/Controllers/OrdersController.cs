using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxDemo.Data;
using OutboxDemo.DTOs;
using OutboxDemo.Models;
using System.Text.Json;

namespace OutboxDemo.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(AppDbContext db, ILogger<OrdersController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            // Begin a transaction
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Create order
                var order = new Order {
                    CustomerId  = dto.CustomerId,
                    TotalAmount = dto.Total,
                    Status      = "PLACED"
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // 2) Add outbox entry
                var evt = new {
                    eventType = "OrderPlaced",
                    data      = new { orderId = order.Id, customerId = dto.CustomerId, total = dto.Total }
                };
                _db.Outbox.Add(new OutboxMessage {
                    AggregateType = "Order",
                    AggregateId   = order.Id,
                    Payload       = JsonSerializer.Serialize(evt),
                    CreatedAt     = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return CreatedAtAction(null, new { order.Id }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to place order");
                await tx.RollbackAsync();
                return StatusCode(500, "Could not place order");
            }
        }
    }
}

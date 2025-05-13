using Microsoft.EntityFrameworkCore;
using OutboxDemo.Data;

namespace OutboxDemo.Services
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcher> _logger;
        private const int BatchSize = 50;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

        public OutboxDispatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DispatchBatchAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }

        private async Task DispatchBatchAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>()!;

                var pending = await db.Outbox
                    .Where(x => !x.Sent)
                    .OrderBy(x => x.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                foreach (var msg in pending)
                {
                    try
                    {
                        _logger.LogInformation("Dispatching OutboxMessage {Id}: {Payload}", msg.Id, msg.Payload);

                        msg.Sent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to dispatch OutboxMessage {Id}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch batch error");
            }
        }
    }
}
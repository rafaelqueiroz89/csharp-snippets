using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OutboxDemo.Data;
using OutboxDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseInMemoryDatabase("OutboxDemo").ConfigureWarnings(w => 
            w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
        ));

builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();

using EfCorePgPoc.Data;
using EfCorePgPoc.DTOs;
using EfCorePgPoc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EventManagementContext>(options => options.UseNpgsql("Host=localhost; Database=ticketing; Username=postgres; Password=postgres"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Seed test data
app.MapPost("/seed", async (EventManagementContext dbContext) =>
{
    var customerId = Guid.NewGuid();
    var ticketTypeId = Guid.NewGuid();

    var ticketType = new TicketType
    {
        Id = ticketTypeId,
        Name = "VIP Ticket",
        Quantity = 100,
        AvailableQuantity = 100,
        Price = 99.99m
    };

    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        CreatedAtUtc = DateTime.UtcNow,
        TotalPrice = 199.98m,
        Currency = "USD",
        OrderItems = [new() {
            Id = Guid.NewGuid(),
            TicketTypeId = ticketTypeId,
            Quantity = 2,
            Price = 99.99m
        }]
    };

    dbContext.TicketTypes.Add(ticketType);
    dbContext.Orders.Add(order);

    await dbContext.SaveChangesAsync();

    return Results.Ok(new { customerId, ticketTypeId });
});

// Scalar Function
app.MapGet("/ticket-types/{ticketTypeId}/available-quantity",
    async (Guid ticketTypeId, EventManagementContext dbContext) =>
    {
        var result = await dbContext.Database.SqlQuery<int>(
            $"""
            SELECT ticketing.tickets_left({ticketTypeId}) AS "Value"
            """
        ).FirstAsync();

        return Results.Ok(new { ticketTypeId, availableQuantity = result });
    }
);

// Table-valued Function
app.MapGet("/customers/{customerId}/order-summary",
    async (Guid customerId, EventManagementContext dbContext) =>
    {
        var orders = await dbContext.Database
            .SqlQuery<OrderSummaryDto>(
                $"""

                SELECT order_id AS OrderId,
                    created_at_utc AS CreatedAtUtc,
                    total_price AS TotalPrice,
                    currency AS Currency,
                    item_count AS ItemCount
                FROM ticketing.customer_order_summary({customerId})
                """
            ).ToListAsync();

        return Results.Ok(orders);
    }
);

// Stored Procedure
app.MapPut("/ticket-types/{ticketTypeId}/adjust-quantity",
    async (Guid ticketTypeId, int delta, EventManagementContext dbContext) =>
    {
        try
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                CALL ticketing.adjust_available_quantity({ticketTypeId}, {delta})
                """
            );

            return Results.Ok(new { message = "Quantity adjusted successfully" });
        }
        catch(Exception e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }
);

app.Run();
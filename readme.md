# 🎫 EF Core PostgreSQL Functions & Procedures POC

A hands-on demonstration exploring the integration between Entity Framework Core and PostgreSQL's native database functions and stored procedures. This project showcases how to leverage database-level operations while maintaining the convenience of EF Core.

## 🌟 What This Project Demonstrates

This POC explores three distinct patterns for working with PostgreSQL and EF Core:

1. **Scalar Functions** - Retrieving single computed values from the database
2. **Table-Valued Functions** - Executing complex queries that return result sets
3. **Stored Procedures** - Performing atomic operations with built-in validation and locking

## 🏗️ Architecture Overview

```
┌─────────────────┐
│   ASP.NET Core  │
│   Minimal API   │
└────────┬────────┘
         │
         │ EF Core 8.0
         │
┌────────▼────────┐
│   PostgreSQL    │
│   Database      │
│                 │
│  • Functions    │
│  • Procedures   │
│  • Schema       │
└─────────────────┘
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Docker (recommended) or PostgreSQL 14+
- Your preferred IDE

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/YOUR_USERNAME/EfCorePgPoc.git
cd EfCorePgPoc
```

2. **Start PostgreSQL using Docker**
```bash
docker run --name ef-postgres-poc \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=ticketing \
  -p 5432:5432 \
  -d postgres:16
```

3. **Restore packages and apply migrations**
```bash
cd EfCorePgPoc
dotnet restore
dotnet ef database update
```

4. **Create database functions and procedures**
```bash
docker exec -i ef-postgres-poc psql -U postgres -d ticketing < Scripts/create_functions.sql
```

5. **Run the application**
```bash
dotnet run
```

6. **Access Swagger UI**
Navigate to `https://localhost:<port>/swagger` to interact with the API.

## 📚 API Endpoints

### Seed Test Data
```http
POST /seed
```
Creates sample ticket types, orders, and order items. Returns customer and ticket type IDs for testing.

### Get Available Tickets (Scalar Function)
```http
GET /ticket-types/{ticketTypeId}/available-quantity
```
Demonstrates calling a simple PostgreSQL scalar function that returns a single value.

### Get Customer Order Summary (Table-Valued Function)
```http
GET /customers/{customerId}/order-summary
```
Showcases a table-valued function that performs joins and aggregations, returning multiple rows.

### Adjust Ticket Quantity (Stored Procedure)
```http
PUT /ticket-types/{ticketTypeId}/adjust-quantity?delta={number}
```
Invokes a stored procedure with row-level locking and business rule validation.

## 🎯 Key Concepts Illustrated

### 1. Scalar Functions with EF Core
```csharp
var result = await dbContext.Database.SqlQuery<int>(
    $"""
    SELECT ticketing.tickets_left({ticketTypeId}) AS "Value"
    """)
    .FirstAsync();
```
- Uses `SqlQuery<T>` for primitive types
- Requires `AS "Value"` alias for mapping
- Automatically parameterizes interpolated values

### 2. Table-Valued Functions
```csharp
var orders = await dbContext.Database
    .SqlQuery<OrderSummaryDto>(
        $"""
        SELECT
            order_id AS OrderId,
            created_at_utc AS CreatedAtUtc,
            total_price AS TotalPrice,
            currency AS Currency,
            item_count AS ItemCount
        FROM ticketing.customer_order_summary({customerId})
        """)
    .ToListAsync();
```
- Maps to DTOs using column aliases
- Handles complex aggregations efficiently
- Returns multiple rows seamlessly

### 3. Stored Procedures with Validation
```csharp
await dbContext.Database.ExecuteSqlAsync(
    $"""
    CALL ticketing.adjust_available_quantity({ticketTypeId}, {delta})
    """);
```
- Uses `ExecuteSqlAsync` for procedures
- Exception handling propagates database errors
- Ensures atomic operations with row locking

## 🔒 Security Notes

### SQL Injection Protection

This project uses EF Core's interpolated string syntax, which might look like string concatenation but is actually safe:

```csharp
$"SELECT * FROM users WHERE id = {userId}"
```

EF Core converts this into a parameterized query:
```sql
SELECT * FROM users WHERE id = @p0
```

The interpolation syntax (`FormattableString`) allows EF Core to separate the SQL template from the parameters, preventing injection attacks.

## 🧪 Testing the POC

1. Start by calling the `/seed` endpoint to populate test data
2. Copy the returned `customerId` and `ticketTypeId` values
3. Test the scalar function with the ticket type ID
4. Query the order summary using the customer ID
5. Try adjusting quantities with different delta values (positive and negative)
6. Verify validation by attempting to reduce quantity below zero

## 📁 Project Structure

```
EfCorePgPoc/
├── Data/
│   └── EventManagementContext.cs    # EF Core DbContext
├── DTOs/
│   └── OrderSummaryDto.cs           # Data transfer objects
├── Models/
│   ├── Order.cs                     # Domain entities
│   ├── OrderItem.cs
│   └── TicketType.cs
├── Scripts/
│   └── create_functions.sql         # PostgreSQL functions & procedures
├── Migrations/                      # EF Core migrations
└── Program.cs                       # API endpoints & configuration
```

## 🛠️ Technologies Used

- **ASP.NET Core 8.0** - Minimal API framework
- **Entity Framework Core 8.0** - ORM and database abstraction
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider
- **PostgreSQL 16** - Relational database with advanced features
- **Docker** - Containerization for easy database setup

## 💡 Why This Approach?

### When to Use Database Functions & Procedures:

✅ **Complex aggregations** - Multiple joins with window functions run faster in SQL  
✅ **Database-specific features** - Full-text search, JSON operators, CTEs  
✅ **Atomic operations** - Row-level locking and transactional consistency  
✅ **Reduce round trips** - Single function call vs. multiple LINQ queries  
✅ **Legacy integration** - Existing database logic doesn't need rewriting

### When to Stick with LINQ:

✅ **Simple queries** - EF Core generates efficient SQL for basic operations  
✅ **Type safety** - Compile-time checking and IntelliSense support  
✅ **Refactoring** - IDE tools work seamlessly with C# code  
✅ **Database agnostic** - Easier to switch database providers

## 🐛 Troubleshooting

**Database connection fails**
- Verify PostgreSQL is running: `docker ps`
- Check port 5432 is not in use by another service
- Confirm connection string in `Program.cs`

**Functions not found**
- Ensure SQL script executed successfully
- Check schema name is `ticketing`
- Verify functions exist: `\df ticketing.*` in psql

**Migration errors**
- Delete existing database: `docker exec ef-postgres-poc psql -U postgres -c "DROP DATABASE ticketing;"`
- Recreate: `docker exec ef-postgres-poc psql -U postgres -c "CREATE DATABASE ticketing;"`
- Rerun migrations: `dotnet ef database update`

## 📖 Learning Resources

This project was inspired by practical patterns for combining EF Core with PostgreSQL's powerful database features. The goal is to demonstrate that you don't have to choose between ORM convenience and database performance.

## 🤝 Contributing

This is a proof-of-concept project for learning purposes.

## 🙏 Acknowledgments

Built as a practical implementation guide for developers exploring the intersection of EF Core and PostgreSQL's native capabilities.

---

**Built with ❤️ for learning and exploration**

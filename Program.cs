using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SqlAssessmentDemo;

if (args.Length == 0 || args is ["--help"])
{
    Console.WriteLine("""
        SQL Assessment Demo - a SQL Server-backed order application

        Set SQL_SERVER to the SQL Server endpoint. Windows authentication is used.
        The dedicated application database is SqlAssessmentDemo.

        Commands:
          init                   Create the dedicated database and seed demo data
          products               List products and available stock
          order <id> <quantity>  Place an order for demo customer 1
          orders                 Show orders and totals from the reporting view

        Example: dotnet run -- order 1 2
        """);
    return 0;
}

var command = args[0];
var productId = 0;
var quantity = 0;
if (!(args is ["init"] or ["products"] or ["orders"]) &&
    !(args is ["order", _, _] &&
      int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out productId) &&
      int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out quantity) &&
      productId > 0 && quantity > 0))
{
    Console.Error.WriteLine("Invalid command. Use --help; product ID and quantity must be positive integers.");
    return 2;
}

var server = Environment.GetEnvironmentVariable("SQL_SERVER");
if (string.IsNullOrWhiteSpace(server))
{
    Console.Error.WriteLine("SQL_SERVER is required. Set it to your installed SQL Server endpoint; see README.md.");
    return 2;
}

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));
var logger = loggerFactory.CreateLogger("SqlAssessmentDemo");
var repository = new OrderRepository(server);
try
{
    switch (command)
    {
        case "init":
            await repository.InitializeAsync(Path.Combine(AppContext.BaseDirectory, "Database", "Setup.sql"));
            Console.WriteLine("SqlAssessmentDemo initialized. Existing orders and stock were preserved.");
            break;
        case "products":
            foreach (var product in await repository.GetProductsAsync())
            {
                Console.WriteLine($"{product.Id,3}  {product.Name,-24}  {product.Price,10:F2}  Stock: {product.Stock}");
            }
            break;
        case "order":
            var orderId = await repository.PlaceOrderAsync(productId, quantity);
            Console.WriteLine($"Created order {orderId} for demo customer 1.");
            break;
        case "orders":
            foreach (var order in await repository.GetOrdersAsync())
            {
                Console.WriteLine($"{order.Id,3}  {order.Customer,-24}  {order.CreatedAt:u}  Total: {order.Total:F2}");
            }
            break;
    }

    return 0;
}
catch (SqlException exception)
{
    logger.LogError("SQL operation failed with error {ErrorNumber}. Check the endpoint, Windows access, database initialization and available stock.", exception.Number);
    return 1;
}
catch (IOException)
{
    logger.LogError("Database initialization script could not be read. Rebuild the sample and check local file permissions.");
    return 1;
}
catch (UnauthorizedAccessException)
{
    logger.LogError("Database initialization script is not accessible. Check local file permissions.");
    return 1;
}

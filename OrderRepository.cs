using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SqlAssessmentDemo;

public sealed record Product(int Id, string Name, decimal Price, int Stock);

public sealed record OrderSummary(int Id, string Customer, DateTime CreatedAt, decimal Total);

public sealed class OrderRepository(string server)
{
    private SqlConnection CreateConnection(string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = true,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 10,
            ApplicationName = "SqlAssessmentDemo",
        };
        return new SqlConnection(builder.ConnectionString);
    }

    public async Task InitializeAsync(string scriptPath)
    {
        var script = await File.ReadAllTextAsync(scriptPath);
        await using var connection = CreateConnection("master");
        await connection.OpenAsync();
        foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection)
            {
                CommandTimeout = 60,
            };
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        await using var connection = CreateConnection("SqlAssessmentDemo");
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT ProductId, Name, UnitPrice, StockQuantity FROM dbo.Products ORDER BY ProductId;",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var products = new List<Product>();
        while (await reader.ReadAsync())
        {
            products.Add(new Product(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(3)));
        }

        return products;
    }

    public async Task<int> PlaceOrderAsync(int productId, int quantity)
    {
        await using var connection = CreateConnection("SqlAssessmentDemo");
        await connection.OpenAsync();
        await using var command = new SqlCommand("dbo.PlaceOrder", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = 1;
        command.Parameters.Add("@ProductId", SqlDbType.Int).Value = productId;
        command.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
        var orderId = command.Parameters.Add("@OrderId", SqlDbType.Int);
        orderId.Direction = ParameterDirection.Output;
        await command.ExecuteNonQueryAsync();
        return (int)orderId.Value;
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync()
    {
        await using var connection = CreateConnection("SqlAssessmentDemo");
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT OrderId, CustomerName, CreatedAt, TotalAmount FROM dbo.OrderSummary ORDER BY OrderId;",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var orders = new List<OrderSummary>();
        while (await reader.ReadAsync())
        {
            orders.Add(new OrderSummary(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2), reader.GetDecimal(3)));
        }

        return orders;
    }
}

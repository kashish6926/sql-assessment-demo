USE master;
GO
IF DB_ID(N'SqlAssessmentDemo') IS NULL
BEGIN
    CREATE DATABASE SqlAssessmentDemo;
END;
GO
USE SqlAssessmentDemo;
GO
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Region nvarchar(50) NOT NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        UnitPrice decimal(12,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (UnitPrice > 0),
        StockQuantity int NOT NULL CONSTRAINT CK_Products_Stock CHECK (StockQuantity >= 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        CustomerId int NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(CustomerId),
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO
IF OBJECT_ID(N'dbo.OrderLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderLines
    (
        OrderLineId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderLines PRIMARY KEY,
        OrderId int NOT NULL CONSTRAINT FK_OrderLines_Orders REFERENCES dbo.Orders(OrderId),
        ProductId int NOT NULL CONSTRAINT FK_OrderLines_Products REFERENCES dbo.Products(ProductId),
        Quantity int NOT NULL CONSTRAINT CK_OrderLines_Quantity CHECK (Quantity > 0),
        UnitPrice decimal(12,2) NOT NULL
    );
    CREATE INDEX IX_OrderLines_OrderId ON dbo.OrderLines(OrderId);
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT dbo.Customers (Name, Region)
    VALUES (N'Demo Customer', N'North'), (N'Sample Customer', N'South');
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT dbo.Products (Name, UnitPrice, StockQuantity)
    VALUES (N'Desk lamp', 29.95, 50), (N'Keyboard', 49.50, 75), (N'Notebook', 5.25, 200);
END;
GO
CREATE OR ALTER VIEW dbo.OrderSummary
AS
    SELECT o.OrderId, c.Name AS CustomerName, o.CreatedAt,
           CAST(SUM(ol.Quantity * ol.UnitPrice) AS decimal(18,2)) AS TotalAmount
    FROM dbo.Orders AS o
    INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
    INNER JOIN dbo.OrderLines AS ol ON ol.OrderId = o.OrderId
    GROUP BY o.OrderId, c.Name, o.CreatedAt;
GO
CREATE OR ALTER PROCEDURE dbo.PlaceOrder
    @CustomerId int,
    @ProductId int,
    @Quantity int,
    @OrderId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity <= 0
    BEGIN
        THROW 50001, 'Quantity must be positive.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @UnitPrice decimal(12,2);
        SELECT @UnitPrice = UnitPrice
        FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE ProductId = @ProductId AND StockQuantity >= @Quantity;

        IF @UnitPrice IS NULL
        BEGIN
            THROW 50002, 'Product does not exist or stock is insufficient.', 1;
        END;

        INSERT dbo.Orders (CustomerId) VALUES (@CustomerId);
        SET @OrderId = CONVERT(int, SCOPE_IDENTITY());

        INSERT dbo.OrderLines (OrderId, ProductId, Quantity, UnitPrice)
        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);

        UPDATE dbo.Products
        SET StockQuantity = StockQuantity - @Quantity
        WHERE ProductId = @ProductId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;
        THROW;
    END CATCH;
END;
GO

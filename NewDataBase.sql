CREATE DATABASE Warehouse_DB_V3;

USE Warehouse_DB_V3;

-- 1. Справочники
CREATE TABLE dbo.Post (
    Post_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Post_Name NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.FIO (
    FIO_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Last_name NVARCHAR(50) NOT NULL,
    First_name NVARCHAR(50) NOT NULL,
    Middle_name NVARCHAR(50) NULL
);

CREATE TABLE dbo.Data_for_authorization (
    Auth_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Login] NVARCHAR(50) NOT NULL UNIQUE,
    [Password] NVARCHAR(200) NOT NULL,
    LastVhod DATETIME NULL
);

CREATE TABLE dbo.Employee (
    Employee_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Post_id INT NOT NULL,
    FIO_id INT NOT NULL,
    Auth_id INT NOT NULL UNIQUE,
    Photo VARBINARY(MAX) NULL, -- Аватарка
    FOREIGN KEY (Post_id) REFERENCES dbo.Post(Post_id),
    FOREIGN KEY (FIO_id) REFERENCES dbo.FIO(FIO_id),
    FOREIGN KEY (Auth_id) REFERENCES dbo.Data_for_authorization(Auth_id)
);

CREATE TABLE dbo.The_supplier (
    provider_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [name] NVARCHAR(100) NOT NULL,
    [address] NVARCHAR(200) NULL,
    telephone NVARCHAR(50) NULL
);

CREATE TABLE dbo.Type_Tovar (
    Type_Tovar_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Type_Tovar_Name NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE dbo.Product (
    product_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    Type_Tovar_id INT NOT NULL,
    FOREIGN KEY (Type_Tovar_id) REFERENCES dbo.Type_Tovar(Type_Tovar_id)
);

-- 2. Склад
CREATE TABLE dbo.Zona (
    Zona_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name_Zona NVARCHAR(50) NOT NULL
);

CREATE TABLE dbo.StorageCell (
    Cell_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Zona_id INT NOT NULL,
    CellCode NVARCHAR(10) NOT NULL,
    FOREIGN KEY (Zona_id) REFERENCES dbo.Zona(Zona_id),
    CONSTRAINT UQ_Cell UNIQUE (Zona_id, CellCode)
);

-- 3. Приёмка
CREATE TABLE dbo.Receipt (
    Receipt_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptNumber NVARCHAR(50) NOT NULL UNIQUE,
    provider_id INT NOT NULL,
    employee_id INT NOT NULL,
    ReceiptDate DATE NOT NULL DEFAULT(GETDATE()),
    TotalSum DECIMAL(10,2) NULL,
    FOREIGN KEY (provider_id) REFERENCES dbo.The_supplier(provider_id),
    FOREIGN KEY (employee_id) REFERENCES dbo.Employee(Employee_id)
);

CREATE TABLE dbo.ReceiptItem (
    ReceiptItem_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Receipt_id INT NOT NULL,
    product_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Price DECIMAL(10,2) NULL,
    ShelfLifeHours INT NOT NULL CHECK (ShelfLifeHours > 0),
    ArrivalDate DATE NOT NULL,
    FOREIGN KEY (Receipt_id) REFERENCES dbo.Receipt(Receipt_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.Lot (
    Lot_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptItem_id INT NOT NULL,
    product_id INT NOT NULL,
    ArrivalDate DATE NOT NULL,
    ShelfLifeHours INT NOT NULL,
    TotalQuantity INT NOT NULL CHECK (TotalQuantity > 0),
    FOREIGN KEY (ReceiptItem_id) REFERENCES dbo.ReceiptItem(ReceiptItem_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.LotPlacement (
    Placement_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Lot_id INT NOT NULL,
    Cell_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PlacedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    PlacedByEmployee_id INT NOT NULL,
    FOREIGN KEY (Lot_id) REFERENCES dbo.Lot(Lot_id),
    FOREIGN KEY (Cell_id) REFERENCES dbo.StorageCell(Cell_id),
    FOREIGN KEY (PlacedByEmployee_id) REFERENCES dbo.Employee(Employee_id)
);

-- 4. Отгрузка
CREATE TABLE dbo.Shipment (
    Shipment_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ShipmentNumber NVARCHAR(50) NOT NULL UNIQUE,
    employee_id INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    Status NVARCHAR(20) NOT NULL DEFAULT('CREATED'),
    FOREIGN KEY (employee_id) REFERENCES dbo.Employee(Employee_id)
);

CREATE TABLE dbo.ShipmentItem (
    ShipmentItem_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Shipment_id INT NOT NULL,
    product_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    FOREIGN KEY (Shipment_id) REFERENCES dbo.Shipment(Shipment_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

CREATE TABLE dbo.ShipmentPick (
    Pick_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ShipmentItem_id INT NOT NULL,
    Lot_id INT NOT NULL,
    Cell_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PickedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    PickedByEmployee_id INT NOT NULL,
    FOREIGN KEY (ShipmentItem_id) REFERENCES dbo.ShipmentItem(ShipmentItem_id),
    FOREIGN KEY (Lot_id) REFERENCES dbo.Lot(Lot_id),
    FOREIGN KEY (Cell_id) REFERENCES dbo.StorageCell(Cell_id),
    FOREIGN KEY (PickedByEmployee_id) REFERENCES dbo.Employee(Employee_id)
);

-- 5. Логи
CREATE TABLE dbo.ActionLog (
    Log_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ActionTime DATETIME NOT NULL DEFAULT(GETDATE()),
    Employee_id INT NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    product_id INT NULL,
    Lot_id INT NULL,
    Cell_id INT NULL,
    Details NVARCHAR(400) NULL,
    FOREIGN KEY (Employee_id) REFERENCES dbo.Employee(Employee_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id),
    FOREIGN KEY (Lot_id) REFERENCES dbo.Lot(Lot_id),
    FOREIGN KEY (Cell_id) REFERENCES dbo.StorageCell(Cell_id)
);


IF OBJECT_ID('dbo.AddIncomingProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.AddIncomingProduct;

CREATE PROCEDURE dbo.AddIncomingProduct
    @ProductName NVARCHAR(100),
    @TypeID INT,
    @ProviderID INT,
    @EmployeeID INT,
    @Quantity INT,
    @Price DECIMAL(10,2),
    @ShelfLifeHours INT,
    @ArrivalDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @ProdID INT;

        SELECT @ProdID = product_id
        FROM dbo.Product
        WHERE [Name] = @ProductName;

        IF @ProdID IS NULL
        BEGIN
            INSERT INTO dbo.Product ([Name], Type_Tovar_id)
            VALUES (@ProductName, @TypeID);

            SET @ProdID = SCOPE_IDENTITY();
        END

        DECLARE @ReceiptID INT;

        INSERT INTO dbo.Receipt (ReceiptNumber, provider_id, employee_id, ReceiptDate, TotalSum)
        VALUES (
            N'REC-' + CONVERT(NVARCHAR(36), NEWID()),
            @ProviderID,
            @EmployeeID,
            @ArrivalDate,
            @Quantity * @Price
        );

        SET @ReceiptID = SCOPE_IDENTITY();

        DECLARE @ReceiptItemID INT;

        INSERT INTO dbo.ReceiptItem (Receipt_id, product_id, Quantity, Price, ShelfLifeHours, ArrivalDate)
        VALUES (
            @ReceiptID,
            @ProdID,
            @Quantity,
            @Price,
            @ShelfLifeHours,
            @ArrivalDate
        );

        SET @ReceiptItemID = SCOPE_IDENTITY();

        DECLARE @LotID INT;

        INSERT INTO dbo.Lot (ReceiptItem_id, product_id, ArrivalDate, ShelfLifeHours, TotalQuantity)
        VALUES (
            @ReceiptItemID,
            @ProdID,
            @ArrivalDate,
            @ShelfLifeHours,
            @Quantity
        );

        SET @LotID = SCOPE_IDENTITY();

        INSERT INTO dbo.ActionLog (ActionTime, Employee_id, ActionType, product_id, Lot_id, Details)
        VALUES (
            GETDATE(),
            @EmployeeID,
            N'INCOMING',
            @ProdID,
            @LotID,
            N'Принят новый товар: ' + @ProductName
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END



USE Warehouse_DB_V3;
GO

ALTER TABLE dbo.ReceiptItem
ALTER COLUMN ArrivalDate DATETIME NOT NULL;

ALTER TABLE dbo.Lot
ALTER COLUMN ArrivalDate DATETIME NOT NULL;

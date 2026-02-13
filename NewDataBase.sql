CREATE DATABASE Warehouse_DB_V3;
GO

USE Warehouse_DB_V3;


/* 1) Справочники
 */

-- Должности/роли (админ/рабочий и т.д.)
CREATE TABLE dbo.Post (
    Post_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Post_Name NVARCHAR(50) NOT NULL
);

-- ФИО
CREATE TABLE dbo.FIO (
    FIO_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Last_name NVARCHAR(50) NOT NULL,
    First_name NVARCHAR(50) NOT NULL,
    Middle_name NVARCHAR(50) NULL
);

-- Авторизация (пользователь)
CREATE TABLE dbo.Data_for_authorization (
    Auth_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Login] NVARCHAR(50) NOT NULL UNIQUE,
    [Password] NVARCHAR(200) NOT NULL,
    LastVhod DATETIME NULL
);

-- Сотрудник
CREATE TABLE dbo.Employee (
    Employee_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Post_id INT NOT NULL,
    FIO_id INT NOT NULL,
    Auth_id INT NOT NULL UNIQUE,
    FOREIGN KEY (Post_id) REFERENCES dbo.Post(Post_id),
    FOREIGN KEY (FIO_id) REFERENCES dbo.FIO(FIO_id),
    FOREIGN KEY (Auth_id) REFERENCES dbo.Data_for_authorization(Auth_id)
);

-- Поставщик
CREATE TABLE dbo.The_supplier (
    provider_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [name] NVARCHAR(100) NOT NULL,
    [address] NVARCHAR(200) NULL,
    telephone NVARCHAR(50) NULL
);


-- Категории/типы товара (Еда/Техника/Химия/Другое)
CREATE TABLE dbo.Type_Tovar (
    Type_Tovar_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Type_Tovar_Name NVARCHAR(100) NOT NULL UNIQUE
);

-- Товар (справочник товаров)
CREATE TABLE dbo.Product (
    product_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    Type_Tovar_id INT NOT NULL,
    -- measurement_id удален, так как он больше не используется в логике
    FOREIGN KEY (Type_Tovar_id) REFERENCES dbo.Type_Tovar(Type_Tovar_id)
);


/* 2) Склад: зоны/ячейки
 */

-- Зона/Сектор (Еда/Техника/Химия/Другое)
CREATE TABLE dbo.Zona (
    Zona_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name_Zona NVARCHAR(50) NOT NULL
);

-- Ячейка хранения (A1, B1 …)
CREATE TABLE dbo.StorageCell (
    Cell_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Zona_id INT NOT NULL,
    CellCode NVARCHAR(10) NOT NULL,  -- A1, B1, ...
    FOREIGN KEY (Zona_id) REFERENCES dbo.Zona(Zona_id),
    CONSTRAINT UQ_Cell UNIQUE (Zona_id, CellCode)
);


/* =========================================
   3) Приёмка (поступление)
========================================= */

-- Документ поступления
CREATE TABLE dbo.Receipt (
    Receipt_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptNumber NVARCHAR(50) NOT NULL UNIQUE,
    provider_id INT NOT NULL,
    employee_id INT NOT NULL,
    ReceiptDate DATE NOT NULL DEFAULT(GETDATE()),
    CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()), 
    TotalSum DECIMAL(10,2) NULL,
    FOREIGN KEY (provider_id) REFERENCES dbo.The_supplier(provider_id),
    FOREIGN KEY (employee_id) REFERENCES dbo.Employee(Employee_id)
);

-- Позиции поступления
CREATE TABLE dbo.ReceiptItem (
    ReceiptItem_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Receipt_id INT NOT NULL,
    product_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    Price DECIMAL(10,2) NULL,
    ShelfLifeHours INT NOT NULL CHECK (ShelfLifeHours > 0),  -- срок хранения в часах
    ArrivalDate DATE NOT NULL,                               -- дата прибытия
    FOREIGN KEY (Receipt_id) REFERENCES dbo.Receipt(Receipt_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

-- Партия (лот)
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

-- Где лежит партия (распределение по ячейкам)
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


/* 4) Отгрузка
 */

-- Документ отгрузки
CREATE TABLE dbo.Shipment (
    Shipment_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ShipmentNumber NVARCHAR(50) NOT NULL UNIQUE,
    employee_id INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
    Status NVARCHAR(20) NOT NULL DEFAULT('CREATED'), -- CREATED / COMPLETED / CANCELED
    FOREIGN KEY (employee_id) REFERENCES dbo.Employee(Employee_id)
);

-- Позиции отгрузки (что хотим отгрузить)
CREATE TABLE dbo.ShipmentItem (
    ShipmentItem_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Shipment_id INT NOT NULL,
    product_id INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    FOREIGN KEY (Shipment_id) REFERENCES dbo.Shipment(Shipment_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

-- Из каких партий/ячеек списали
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


/* 5) Логи действий
 */

CREATE TABLE dbo.ActionLog (
    Log_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ActionTime DATETIME NOT NULL DEFAULT(GETDATE()),
    Employee_id INT NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,   -- INCOMING / SORT / SHIPMENT / LOGIN etc.
    product_id INT NULL,
    Lot_id INT NULL,
    Cell_id INT NULL,
    Details NVARCHAR(400) NULL,
    FOREIGN KEY (Employee_id) REFERENCES dbo.Employee(Employee_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id),
    FOREIGN KEY (Lot_id) REFERENCES dbo.Lot(Lot_id),
    FOREIGN KEY (Cell_id) REFERENCES dbo.StorageCell(Cell_id)
);


/* 6) Процедурки 
 */

CREATE PROCEDURE [dbo].[AddIncomingProduct]
    -- Входящие параметры
    @ProductName NVARCHAR(100),
    @TypeID INT,            
    @ProviderID INT,        
    @EmployeeID INT,        
    @Quantity INT,          
    @Price DECIMAL(10,2),   
    @ShelfLifeHours INT    
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION; -- Начинаем транзакцию

    BEGIN TRY
        -- =============================================
        -- 1. ЛОГИКА ТОВАРА (Проверка существования)
        -- =============================================
        DECLARE @ProdID INT;

        SELECT @ProdID = product_id 
        FROM dbo.Product 
        WHERE [Name] = @ProductName;

        -- Если товара нет -> Создаем новый
        IF @ProdID IS NULL
        BEGIN
            INSERT INTO dbo.Product ([Name], Type_Tovar_id)
            VALUES (@ProductName, @TypeID);

            SET @ProdID = SCOPE_IDENTITY();
        END

        -- =============================================
        -- 2. СОЗДАЕМ НАКЛАДНУЮ
        -- =============================================
        DECLARE @ReceiptID INT;
        
        INSERT INTO dbo.Receipt (ReceiptNumber, provider_id, employee_id, TotalSum)
        VALUES ('REC-' + CAST(NEWID() AS NVARCHAR(36)), @ProviderID, @EmployeeID, (@Quantity * @Price));
        
        SET @ReceiptID = SCOPE_IDENTITY();

        -- =============================================
        -- 3. ЗАПИСЫВАЕМ СТРОКУ (Связь товара и накладной)
        -- =============================================
        DECLARE @ReceiptItemID INT;

        INSERT INTO dbo.ReceiptItem (Receipt_id, product_id, Quantity, Price, ShelfLifeHours, ArrivalDate)
        VALUES (@ReceiptID, @ProdID, @Quantity, @Price, @ShelfLifeHours, CAST(GETDATE() AS DATE));

        SET @ReceiptItemID = SCOPE_IDENTITY();

        -- =============================================
        -- 4. СОЗДАЕМ ПАРТИЮ (LOT)
        -- =============================================
        -- Исправлено: убраны ExpireAt, RemainingQuantity и Status, так как их больше нет в таблице dbo.Lot
        DECLARE @LotID INT;

        INSERT INTO dbo.Lot (ReceiptItem_id, product_id, ArrivalDate, ShelfLifeHours, TotalQuantity)
        VALUES (
            @ReceiptItemID,
            @ProdID,
            CAST(GETDATE() AS DATE),
            @ShelfLifeHours,
            @Quantity
        );

        SET @LotID = SCOPE_IDENTITY();

        -- =============================================
        -- 5. Логируем
        -- =============================================
        INSERT INTO dbo.ActionLog (
            ActionTime,
            Employee_id,
            ActionType,
            product_id,
            Lot_id,
            Cell_id, -- Будет NULL, так как мы пока не кладем в ячейку
            Details
        )
        VALUES (
            GETDATE(),
            @EmployeeID,
            N'INCOMING', -- Тип действия "Приемка"
            @ProdID,
            @LotID,
            NULL, 
            N'Принят новый товар: ' + @ProductName + N', Кол-во: ' + CAST(@Quantity AS NVARCHAR(20)) + N', Цена: ' + CAST(@Price AS NVARCHAR(20))
        );

        -- Если всё прошло хорошо — сохраняем
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- Если ошибка — отменяем всё
        ROLLBACK TRANSACTION;
        THROW; 
    END CATCH
END

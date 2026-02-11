CREATE DATABASE Warehouse_DB_V3;

USE Warehouse_DB_V3;


/* 
   1) Справочники
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


-- Единицы измерения
CREATE TABLE dbo.unit_of_measurement (
    measurement_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
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
    measurement_id INT NULL,
    FOREIGN KEY (Type_Tovar_id) REFERENCES dbo.Type_Tovar(Type_Tovar_id),
    FOREIGN KEY (measurement_id) REFERENCES dbo.unit_of_measurement(measurement_id)
);


/* 
   2) Склад: зоны/ячейки
 */

-- Склад (может быть один)
CREATE TABLE dbo.Warehouse (
    Warehouse_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Address NVARCHAR(200) NULL
);

-- Зона/Сектор (Еда/Техника/Химия/Другое)
CREATE TABLE dbo.Zona (
    Zona_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Warehouse_id INT NOT NULL,
    Name_Zona NVARCHAR(50) NOT NULL,
    FOREIGN KEY (Warehouse_id) REFERENCES dbo.Warehouse(Warehouse_id),
    CONSTRAINT UQ_Zona UNIQUE (Warehouse_id, Name_Zona)
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
   Логика:
   Receipt = документ поступления (накладная)
   ReceiptItem = позиции в документе
   Lot = партия на складе (срок хранения, дата)
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
    ShelfLifeHours INT NOT NULL CHECK (ShelfLifeHours > 0),  -- срок хранения в часах (как у тебя)
    ArrivalDate DATE NOT NULL,                                -- дата прибытия
    FOREIGN KEY (Receipt_id) REFERENCES dbo.Receipt(Receipt_id),
    FOREIGN KEY (product_id) REFERENCES dbo.Product(product_id)
);

-- Партия (лот) — именно она “живет” на складе и имеет срок хранения
CREATE TABLE dbo.Lot (
    Lot_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptItem_id INT NOT NULL,
    product_id INT NOT NULL,
    ArrivalDate DATE NOT NULL,
    ShelfLifeHours INT NOT NULL,
    ExpireAt DATETIME NOT NULL,     -- ArrivalDate + ShelfLifeHours
    TotalQuantity INT NOT NULL CHECK (TotalQuantity > 0),
    RemainingQuantity INT NOT NULL CHECK (RemainingQuantity >= 0),
    Status NVARCHAR(20) NOT NULL DEFAULT('NEW'), -- NEW / STORED / SHIPPED / EXPIRED
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


/* 
   4) Отгрузка
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

-- Из каких партий/ячеек списали (чтобы реально уменьшать остатки)
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


/* 
   5) Логи действий
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


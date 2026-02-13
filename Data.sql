USE Warehouse_DB_V3;


/* ======================================================
   1. ЗАПОЛНЕНИЕ СПРАВОЧНИКОВ И СОТРУДНИКОВ
   ====================================================== */

-- 1.1 Должности
INSERT INTO dbo.Post (Post_Name) VALUES 
(N'Администратор'), 
(N'Старший рабочий'), 
(N'Рабочий');

-- 1.2 ФИО сотрудников
INSERT INTO dbo.FIO (Last_name, First_name, Middle_name) VALUES 
(N'Иванов', N'Иван', N'Иванович'),       -- Будет Админ
(N'Петров', N'Петр', N'Петрович'),       -- Будет Старший рабочий
(N'Сидоров', N'Сидор', N'Сидорович');    -- Будет Рабочий

-- 1.3 Данные для авторизации (Логин / Пароль)
INSERT INTO dbo.Data_for_authorization ([Login], [Password], LastVhod) VALUES 
('admin', 'admin123', GETDATE()), 
('senior', 'senior123', GETDATE()), 
('worker', 'worker123', GETDATE());

-- 1.4 Создаем Сотрудников (Связываем ФИО + Должность + Логин)
-- Post_id: 1=Админ, 2=Старший, 3=Рабочий
INSERT INTO dbo.Employee (Post_id, FIO_id, Auth_id) VALUES 
(1, 1, 1), -- Иванов (Админ)
(2, 2, 2), -- Петров (Старший)
(3, 3, 3); -- Сидоров (Рабочий)

-- 1.5 Поставщики
INSERT INTO dbo.The_supplier ([name], [address], telephone) VALUES 
(N'ООО "Фермер Про"', N'г. Москва, ул. Ленина 1', N'+7(999)111-22-33'),
(N'АО "ТехноМир"', N'г. Казань, ул. Пушкина 10', N'+7(999)444-55-66');

-- 1.6 Типы товаров
INSERT INTO dbo.Type_Tovar (Type_Tovar_Name) VALUES 
(N'Еда'), (N'Техника'), (N'Химия'), (N'Другое');

-- 1.7 Товары
-- Убрал measurement_id, так как мы удалили эту логику из таблицы Product
-- Type_Tovar_id: 1=Еда, 2=Техника
INSERT INTO dbo.Product ([Name], Type_Tovar_id) VALUES 
(N'Молоко Домик в деревне', 1),    
(N'Ноутбук ASUS', 2),             
(N'Яблоки Гренни', 1);            


/* ======================================================
   2. СКЛАДСКАЯ СТРУКТУРА
   ====================================================== */

-- 2.1 Зоны
-- Убрал Warehouse_id, так как мы отказались от таблицы Warehouse
INSERT INTO dbo.Zona (Name_Zona) VALUES 
(N'Зона А (Продукты)'), 
(N'Зона Б (Электроника)');

-- 2.2 Ячейки
-- Zona_id: 1=Продукты, 2=Электроника
INSERT INTO dbo.StorageCell (Zona_id, CellCode) VALUES 
(1, N'A-01'), (1, N'A-02'), -- Ячейки для еды
(2, N'B-01'), (2, N'B-02'); -- Ячейки для техники


/* ======================================================
   3. ПРИЁМКА ТОВАРА
   ====================================================== */

-- 3.1 Создаем накладную (Принимает Старший рабочий - ID 2)
INSERT INTO dbo.Receipt (ReceiptNumber, provider_id, employee_id, TotalSum) VALUES 
(N'REC-2024-001', 1, 2, 50000.00);

-- 3.2 Добавляем товары в накладную (Купили 100 Яблок, Product_id = 3)
INSERT INTO dbo.ReceiptItem (Receipt_id, product_id, Quantity, Price, ShelfLifeHours, ArrivalDate) VALUES 
(1, 3, 100, 150.00, 720, CAST(GETDATE() AS DATE)); -- 720 часов = 30 дней срок

-- 3.3 Система создает Партию (LOT) на основе строки приёмки
-- Убрал ExpireAt, RemainingQuantity и Status, так как мы их удалили из таблицы Lot
INSERT INTO dbo.Lot (ReceiptItem_id, product_id, ArrivalDate, ShelfLifeHours, TotalQuantity) VALUES 
(1, 3, CAST(GETDATE() AS DATE), 720, 100);

-- 3.4 Рабочий (ID 3) размещает партию в ячейку
-- Кладем все 100 яблок (Lot_id 1) в ячейку A-01 (Cell_id 1)
INSERT INTO dbo.LotPlacement (Lot_id, Cell_id, Quantity, PlacedByEmployee_id) VALUES 
(1, 1, 100, 3);


/* ======================================================
   4. ОТГРУЗКА
   ====================================================== */

-- 4.1 Админ (ID 1) создает документ на отгрузку
INSERT INTO dbo.Shipment (ShipmentNumber, employee_id, Status) VALUES 
(N'SHP-2024-999', 1, N'COMPLETED');

-- 4.2 Что хотим отгрузить: 10 яблок
INSERT INTO dbo.ShipmentItem (Shipment_id, product_id, Quantity) VALUES 
(1, 3, 10);

-- 4.3 Физический подбор товара
-- Рабочий (ID 3) берет 10 яблок из ячейки A-01, из партии (Lot_id 1)
INSERT INTO dbo.ShipmentPick (ShipmentItem_id, Lot_id, Cell_id, Quantity, PickedByEmployee_id) VALUES 
(1, 1, 1, 10, 3);

-- ПРИМЕЧАНИЕ: Обновление RemainingQuantity в dbo.Lot больше не требуется, 
-- так как мы высчитываем актуальные остатки динамически (TotalQuantity - отгруженное).


/* ======================================================
   5. ЛОГИРОВАНИЕ
   ====================================================== */

INSERT INTO dbo.ActionLog (Employee_id, ActionType, product_id, Lot_id, Cell_id, Details) VALUES 
(2, N'INCOMING', NULL, NULL, NULL, N'Создана накладная №REC-2024-001'),
(3, N'SORT', 3, 1, 1, N'Размещено 100 шт яблок в ячейку A-01'),
(1, N'SHIPMENT_CREATE', NULL, NULL, NULL, N'Создан заказ на отгрузку SHP-2024-999'),
(3, N'PICKING', 3, 1, 1, N'Отобрано 10 шт яблок для заказа');
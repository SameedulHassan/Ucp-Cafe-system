USE FirstDB;
GO

IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    CREATE TABLE Users
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL DEFAULT 'User',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;
GO

IF COL_LENGTH('Users', 'Role') IS NULL
    ALTER TABLE Users ADD Role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'User';
GO

IF COL_LENGTH('Users', 'CreatedAt') IS NULL
    ALTER TABLE Users ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE();
GO

IF OBJECT_ID('CafeItems', 'U') IS NULL
CREATE TABLE CafeItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(60) NOT NULL DEFAULT 'Other',
    Price DECIMAL(10,2) NOT NULL,
    ImagePath NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('ItemAvailabilities', 'U') IS NULL
CREATE TABLE ItemAvailabilities
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CafeItemId INT NOT NULL,
    AvailableDate DATE NOT NULL,
    IsAvailable BIT NOT NULL,
    CONSTRAINT FK_ItemAvailabilities_CafeItems FOREIGN KEY (CafeItemId)
        REFERENCES CafeItems(Id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('Reviews', 'U') IS NULL
CREATE TABLE Reviews
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CafeItemId INT NOT NULL,
    UserId INT NOT NULL,
    Rating INT NOT NULL,
    Comment NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reviews_CafeItems FOREIGN KEY (CafeItemId) REFERENCES CafeItems(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('Orders', 'U') IS NULL
CREATE TABLE Orders
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    TotalAmount DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

IF OBJECT_ID('OrderItems', 'U') IS NULL
CREATE TABLE OrderItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    CafeItemId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_CafeItems FOREIGN KEY (CafeItemId) REFERENCES CafeItems(Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@ucpcafe.local')
    INSERT INTO Users(Name, Email, Password, Role)
    VALUES ('Cafe Administrator', 'admin@ucpcafe.local', 'admin123', 'Admin');
GO

IF OBJECT_ID('Deals', 'U') IS NULL
CREATE TABLE Deals
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    DealPrice DECIMAL(10,2) NOT NULL,
    ImagePath NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('DealItems', 'U') IS NULL
CREATE TABLE DealItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DealId INT NOT NULL,
    CafeItemId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DealItems_Deals FOREIGN KEY (DealId) REFERENCES Deals(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DealItems_CafeItems FOREIGN KEY (CafeItemId) REFERENCES CafeItems(Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM Deals)
BEGIN
    INSERT INTO Deals(Name, Description, DealPrice, ImagePath, IsActive)
    VALUES ('Student Combo', 'Chicken Burger + Fries + Cold Coffee at a student-friendly price.', 850.00, '', 1);

    DECLARE @DealId INT = SCOPE_IDENTITY();
    INSERT INTO DealItems(DealId, CafeItemId, Quantity)
    SELECT @DealId, Id, 1 FROM CafeItems WHERE Name IN ('Chicken Burger', 'French Fries', 'Cold Coffee');
END;
GO

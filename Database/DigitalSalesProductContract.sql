SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductContract', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesProductContract
    (
        SalesProductContractID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RM_DigitalSalesProductContract PRIMARY KEY,
        SalesProductID INT NOT NULL,
        ContractID INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesProductContract_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesProductContract_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL,
        CONSTRAINT FK_RM_DigitalSalesProductContract_Product FOREIGN KEY (SalesProductID) REFERENCES dbo.RM_DigitalSalesProduct(SalesProductID),
        CONSTRAINT FK_RM_DigitalSalesProductContract_Contract FOREIGN KEY (ContractID) REFERENCES dbo.RM_Contracts(ContractID)
    );
    CREATE UNIQUE INDEX UX_RM_DigitalSalesProductContract_Active
        ON dbo.RM_DigitalSalesProductContract(SalesProductID, ContractID)
        WHERE IsDeleted = 0;
    CREATE INDEX IX_RM_DigitalSalesProductContract_Product
        ON dbo.RM_DigitalSalesProductContract(SalesProductID, IsDeleted);
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductContract_GetByProductID', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductContract_GetByProductID AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductContract_GetByProductID
    @SalesProductID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ContractID, c.ProductProjectID, c.ContractCode, c.ContractName,
           ds.CustomerID, cu.CustomerName, c.SignDate, c.StartDate, c.EndDate,
           c.ContractValue, c.VAT, c.TotalAmount, c.StatusContract,
           c.BillingCycleID, c.ReminderType, c.ReminderDayOfMonth,
           st.StatusName, bc.CycleName
    FROM dbo.RM_DigitalSalesProductContract link
    INNER JOIN dbo.RM_DigitalSalesProduct product ON product.SalesProductID = link.SalesProductID
    INNER JOIN dbo.RM_DigitalSales ds ON ds.DigitalSalesID = product.DigitalSalesID
    INNER JOIN dbo.RM_Contracts c ON c.ContractID = link.ContractID
    LEFT JOIN dbo.RM_Customer cu ON cu.CustomerID = ds.CustomerID
    LEFT JOIN dbo.RM_Status st ON st.ID = c.StatusContract
    LEFT JOIN dbo.RM_BillingCycles bc ON bc.BillingCycleID = c.BillingCycleID
    WHERE link.SalesProductID = @SalesProductID
      AND link.IsDeleted = 0
      AND ISNULL(c.IsDeleted, 0) = 0
    ORDER BY c.SignDate DESC, c.ContractID DESC;
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductContract_Link', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductContract_Link AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductContract_Link
    @SalesProductID INT,
    @ContractID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct WHERE SalesProductID = @SalesProductID AND IsDeleted = 0)
       OR NOT EXISTS (SELECT 1 FROM dbo.RM_Contracts WHERE ContractID = @ContractID AND ISNULL(IsDeleted, 0) = 0)
        RETURN -1;
    UPDATE dbo.RM_DigitalSalesProductContract
    SET IsDeleted = 0, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
    WHERE SalesProductID = @SalesProductID AND ContractID = @ContractID;
    IF @@ROWCOUNT = 0
        INSERT INTO dbo.RM_DigitalSalesProductContract(SalesProductID, ContractID, CreatedBy)
        VALUES (@SalesProductID, @ContractID, @UserName);
    RETURN @ContractID;
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductContract_Unlink', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductContract_Unlink AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductContract_Unlink
    @SalesProductID INT,
    @ContractID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.RM_DigitalSalesProductContract
    SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
    WHERE SalesProductID = @SalesProductID AND ContractID = @ContractID AND IsDeleted = 0;
    RETURN @@ROWCOUNT;
END;
GO

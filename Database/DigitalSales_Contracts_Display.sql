/* Hiển thị hợp đồng liên kết DigitalSales tại Cate/RM_Contracts. */
ALTER PROCEDURE dbo.RM_Contracts_Get
    @Keyword NVARCHAR(250) = NULL, @StatusID INT = NULL, @Search NVARCHAR(250),
    @Order VARCHAR(3), @OrderDir VARCHAR(10), @PageIndex INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Order = ISNULL(@Order, '0'); SET @OrderDir = UPPER(ISNULL(@OrderDir, 'ASC'));
    SET @PageIndex = ISNULL(@PageIndex, 0); SET @PageSize = ISNULL(@PageSize, 10);
    SET @Search = ISNULL(RTRIM(LTRIM(@Search)), '');

    ;WITH SourceData AS
    (
        SELECT c.ContractID, c.ProductProjectID, c.ContractCode, c.ContractName,
               COALESCE(psProject.NameProduct, psDigital.NameProduct) AS NameProduct,
               COALESCE(project.CustomerID, sales.CustomerID) AS CustomerID,
               customer.CustomerName, c.SignDate, c.StartDate, c.EndDate,
               c.ContractValue, c.VAT, c.TotalAmount, c.StatusContract,
               status.StatusName, status.StatusClass,
               users.FullName + ' (' + users.UserName + ')' AS UserCreated,
               sales.DigitalSalesID, sales.Code AS DigitalSalesCode, sales.Title AS DigitalSalesName
        FROM dbo.RM_Contracts c
        LEFT JOIN dbo.RM_ProductProject pp ON pp.ProductProjectID = c.ProductProjectID
        LEFT JOIN dbo.RM_Project project ON project.ProjectID = pp.ProjectID
        LEFT JOIN dbo.RM_ProductService psProject ON psProject.ProductServiceID = pp.ProductServiceID
        OUTER APPLY
        (
            SELECT TOP (1) dsp.DigitalSalesID, dsp.ProductServiceID
            FROM dbo.RM_DigitalSalesProductContract link
            INNER JOIN dbo.RM_DigitalSalesProduct dsp ON dsp.SalesProductID = link.SalesProductID AND dsp.IsDeleted = 0
            WHERE link.ContractID = c.ContractID AND link.IsDeleted = 0
            ORDER BY link.SalesProductContractID DESC
        ) digitalLink
        LEFT JOIN dbo.RM_DigitalSales sales ON sales.DigitalSalesID = digitalLink.DigitalSalesID AND sales.IsDeleted = 0
        LEFT JOIN dbo.RM_ProductService psDigital ON psDigital.ProductServiceID = digitalLink.ProductServiceID
        LEFT JOIN dbo.RM_Customer customer ON customer.CustomerID = COALESCE(project.CustomerID, sales.CustomerID)
        LEFT JOIN dbo.RM_Status status ON status.ID = c.StatusContract
        LEFT JOIN dbo.Sys_Users users ON users.UserName = c.CreatedBy
        WHERE c.IsDeleted = 0
          -- Trang Cate/RM_Contracts chỉ quản lý hợp đồng phát sinh từ DigitalSales.
          AND sales.DigitalSalesID IS NOT NULL
          AND (@StatusID IS NULL OR c.StatusContract = @StatusID)
          AND (@Search = '' OR c.ContractCode COLLATE Latin1_General_CI_AI LIKE N'%' + @Search + '%'
               OR c.ContractName COLLATE Latin1_General_CI_AI LIKE N'%' + @Search + '%'
               OR customer.CustomerName COLLATE Latin1_General_CI_AI LIKE N'%' + @Search + '%'
               OR sales.Code COLLATE Latin1_General_CI_AI LIKE N'%' + @Search + '%'
               OR sales.Title COLLATE Latin1_General_CI_AI LIKE N'%' + @Search + '%')
          AND (@Keyword IS NULL OR c.ContractCode COLLATE Latin1_General_CI_AI LIKE N'%' + @Keyword + '%'
               OR c.ContractName COLLATE Latin1_General_CI_AI LIKE N'%' + @Keyword + '%'
               OR customer.CustomerName COLLATE Latin1_General_CI_AI LIKE N'%' + @Keyword + '%'
               OR sales.Code COLLATE Latin1_General_CI_AI LIKE N'%' + @Keyword + '%'
               OR sales.Title COLLATE Latin1_General_CI_AI LIKE N'%' + @Keyword + '%')
    ), Ranked AS
    (
        SELECT *, ROW_NUMBER() OVER (ORDER BY
            CASE WHEN @OrderDir = 'ASC' AND @Order = '0' THEN ContractID END ASC,
            CASE WHEN @OrderDir = 'ASC' AND @Order = '1' THEN ContractCode END ASC,
            CASE WHEN @OrderDir = 'ASC' AND @Order = '2' THEN ContractName END ASC,
            CASE WHEN @OrderDir = 'ASC' AND @Order = '3' THEN CustomerName END ASC,
            CASE WHEN @OrderDir = 'DESC' AND @Order = '0' THEN ContractID END DESC,
            CASE WHEN @OrderDir = 'DESC' AND @Order = '1' THEN ContractCode END DESC,
            CASE WHEN @OrderDir = 'DESC' AND @Order = '2' THEN ContractName END DESC,
            CASE WHEN @OrderDir = 'DESC' AND @Order = '3' THEN CustomerName END DESC,
            ContractID DESC) AS RowIndex
        FROM SourceData
    )
    SELECT *, (SELECT COUNT(1) FROM Ranked) AS TotalRow FROM Ranked
    WHERE @PageSize <= 0 OR RowIndex BETWEEN @PageIndex + 1 AND @PageIndex + @PageSize;
END
GO

ALTER PROCEDURE dbo.RM_Contracts_GetById @ContractID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.*, COALESCE(customer.CustomerName, '') AS CustomerName,
           COALESCE(psProject.NameProduct, psDigital.NameProduct) AS NameProduct,
           status.StatusName, status.StatusClass, users.FullName + ' (' + users.UserName + ')' AS UserCreated,
           ISNULL(billing.CycleName, '') AS CycleName,
           sales.DigitalSalesID, sales.Code AS DigitalSalesCode, sales.Title AS DigitalSalesName
    FROM dbo.RM_Contracts c
    LEFT JOIN dbo.RM_ProductProject pp ON pp.ProductProjectID = c.ProductProjectID
    LEFT JOIN dbo.RM_Project project ON project.ProjectID = pp.ProjectID
    LEFT JOIN dbo.RM_ProductService psProject ON psProject.ProductServiceID = pp.ProductServiceID
    OUTER APPLY
    (
        SELECT TOP (1) dsp.DigitalSalesID, dsp.ProductServiceID
        FROM dbo.RM_DigitalSalesProductContract link
        INNER JOIN dbo.RM_DigitalSalesProduct dsp ON dsp.SalesProductID = link.SalesProductID AND dsp.IsDeleted = 0
        WHERE link.ContractID = c.ContractID AND link.IsDeleted = 0
        ORDER BY link.SalesProductContractID DESC
    ) digitalLink
    LEFT JOIN dbo.RM_DigitalSales sales ON sales.DigitalSalesID = digitalLink.DigitalSalesID AND sales.IsDeleted = 0
    LEFT JOIN dbo.RM_ProductService psDigital ON psDigital.ProductServiceID = digitalLink.ProductServiceID
    LEFT JOIN dbo.RM_Customer customer ON customer.CustomerID = COALESCE(project.CustomerID, sales.CustomerID)
    LEFT JOIN dbo.RM_Status status ON status.ID = c.StatusContract
    LEFT JOIN dbo.Sys_Users users ON users.UserName = c.CreatedBy
    LEFT JOIN dbo.RM_BillingCycles billing ON billing.BillingCycleID = c.BillingCycleID
    WHERE c.ContractID = @ContractID AND c.IsDeleted = 0;
END
GO

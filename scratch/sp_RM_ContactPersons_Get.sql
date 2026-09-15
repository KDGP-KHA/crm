
CREATE PROCEDURE [dbo].[RM_ContactPersons_Get]
    @Keyword    NVARCHAR(200) = NULL,
    @Gender     INT = NULL,
    @Status     INT = NULL,
    @CustomerID INT = NULL,
    @PageNumber INT = 1,
    @PageSize   INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
    cc.ContactPersonID,
    STUFF((
        SELECT ' | ' + c2.CustomerName 
                     + N'::' + ISNULL(cc2.Position, N'')
                     + N'::' + ISNULL(cc2.Email, N'')  
        FROM RM_CustomerContact cc2
        INNER JOIN RM_Customer c2 
            ON cc2.CustomerID = c2.CustomerID 
            AND c2.IsDeleted = 0
        WHERE cc2.ContactPersonID = cc.ContactPersonID
            AND cc2.IsDeleted = 0
            AND c2.CustomerName IS NOT NULL
            AND LTRIM(RTRIM(c2.CustomerName)) <> ''
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 3, '') AS CustomerName
INTO #CustomerNames
FROM RM_CustomerContact cc
INNER JOIN RM_Customer c 
    ON cc.CustomerID = c.CustomerID 
    AND c.IsDeleted = 0
WHERE cc.IsDeleted = 0
GROUP BY cc.ContactPersonID;

    SELECT DISTINCT cp.ContactPerson_ID
    INTO #FilteredIDs
    FROM RM_ContactPersons cp
    LEFT JOIN RM_CustomerContact cc 
        ON cp.ContactPerson_ID = cc.ContactPersonID AND cc.IsDeleted = 0
    LEFT JOIN RM_Customer c 
        ON cc.CustomerID = c.CustomerID AND c.IsDeleted = 0
    WHERE cp.IsDeleted = 0
        AND (@Keyword IS NULL OR 
             cp.FullName    COLLATE Latin1_General_CI_AI LIKE '%' + @Keyword + '%' OR
             c.CustomerName COLLATE Latin1_General_CI_AI LIKE '%' + @Keyword + '%' OR
             c.ShortName    COLLATE Latin1_General_CI_AI LIKE '%' + @Keyword + '%')
        AND (@Gender     IS NULL OR cp.Gender      = @Gender)
        AND (@Status     IS NULL OR cp.Status      = @Status)
        AND (@CustomerID IS NULL OR cc.CustomerID  = @CustomerID);

    SELECT 
        cp.*,
        cn.CustomerName,
        COUNT(*) OVER() AS TotalRow
    INTO #TempData
    FROM RM_ContactPersons cp
    INNER JOIN #FilteredIDs f  ON cp.ContactPerson_ID = f.ContactPerson_ID
    LEFT JOIN  #CustomerNames cn ON cp.ContactPerson_ID = cn.ContactPersonID;

    IF (@PageSize IS NULL OR @PageSize <= 0)
    BEGIN
        SELECT * FROM #TempData ORDER BY ContactPerson_ID DESC;
    END
    ELSE
    BEGIN
        SELECT * FROM #TempData
        ORDER BY ContactPerson_ID DESC
        OFFSET (@PageNumber - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY;
    END

    DROP TABLE #CustomerNames;
    DROP TABLE #FilteredIDs;
    DROP TABLE #TempData;
END

CREATE OR ALTER PROCEDURE dbo.USP_Top5_Defects_CurrentMonth
    @ShopId INT,
    @WeekStart DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 5
        DD.Defect_Name,
        COUNT(*) AS Defect_Count
    FROM dbo.Partwise_Defect_Master AS HH
    INNER JOIN dbo.MM_Qlty_Defect AS DD
        ON HH.Defect_ID = DD.Defect_ID
    WHERE HH.Shop_ID = @ShopId
        AND HH.Is_Part_Based = 1
        AND HH.Inserted_Date >= @WeekStart
        AND HH.Inserted_Date < DATEADD(DAY, 7, @WeekStart)
    GROUP BY DD.Defect_Name
    ORDER BY COUNT(*) DESC;
END;
GO

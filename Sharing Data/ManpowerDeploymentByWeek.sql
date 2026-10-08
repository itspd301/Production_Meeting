CREATE OR ALTER PROCEDURE dbo.USP_ManpowerDeploymentByWeek
    @ShopId INT,
    @WeekStart DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDate DATETIME = CONVERT(DATETIME, @WeekStart);
    DECLARE @ToDate DATETIME = DATEADD(DAY, 7, @FromDate);

    SELECT
        CASE
            WHEN shif.Shift_Name = 'First Shift' THEN '1st'
            WHEN shif.Shift_Name = 'Second Shift' THEN '2nd'
            WHEN shif.Shift_Name = 'Third Shift' THEN '3rd'
            ELSE shif.Shift_Name
        END AS ShiftName,
        CONVERT(DATE, RowData.Allocation_Date) AS ShiftDate,
        SUM(RowData.Deployement_Count) AS AssociateCount,
        SUM(RowData.RequiredWorkStation) AS RequiredWorkstations
    FROM
    (
        SELECT
            Requirement.Shop_ID,
            Requirement.Line_ID,
            Requirement.Setup_ID,
            Requirement.Shift_ID,
            Requirement.RequiredMapower,
            Deployement.Deployement_Count,
            Deployement.Allocation_Date,
            Deployement.Abs_Manpower_Count,
            EmptyWorkstation.EmptyWorkstation,
            EmptyWorkstation.RequiredWorkStation
        FROM
        (
            SELECT
                daywise.Line_ID,
                daywise.Setup_ID,
                daywise.Shop_ID,
                daywise.Shift_ID,
                SUM(DISTINCT req.Direct_MP_Quantity) AS RequiredMapower
            FROM MM_ManPower_Required AS req WITH (NOLOCK)
            INNER JOIN MM_AM_Setup_Daywise AS daywise WITH (NOLOCK)
                ON req.Line_ID = daywise.Line_ID
               AND req.Setup_ID = daywise.Setup_ID
            INNER JOIN MM_Station_Setup_Mapping AS map WITH (NOLOCK)
                ON map.Setup_ID = daywise.Setup_ID
            INNER JOIN MM_AM_WorkStation AS workstn WITH (NOLOCK)
                ON map.Station_ID = workstn.Station_ID
            WHERE daywise.Setup_Date >= @FromDate
              AND daywise.Setup_Date < @ToDate
              AND req.Shop_ID = @ShopId
              AND daywise.Shop_ID = @ShopId
              AND daywise.Is_Active = 1
            GROUP BY daywise.Line_ID, daywise.Setup_ID, daywise.Shop_ID, daywise.Shift_ID
        ) AS Requirement
        INNER JOIN
        (
            SELECT
                Deploy.Line_ID,
                Deploy.Allocation_Date,
                Deploy.Setup_ID,
                Deploy.Shop_ID,
                Deploy.Shift_ID,
                ISNULL(Deploy.Deployement_Count, 0) + ISNULL(DirectDeployement.DirectDeployement_Count, 0) AS Deployement_Count,
                ISNULL(AbsDeploy.Abs_Deployement_Count, 0) AS Abs_Manpower_Count
            FROM
            (
                SELECT
                    allocation.Line_ID,
                    allocation.Setup_ID,
                    allocation.Shop_ID,
                    allocation.Shift_ID,
                    CONVERT(DATE, allocation.Allocation_Date) AS Allocation_Date,
                    COUNT(DISTINCT allocation.OSM_ID) AS Deployement_Count
                FROM MM_AM_Operator_Station_Allocation AS allocation WITH (NOLOCK)
                INNER JOIN MM_Station_Setup_Mapping AS map WITH (NOLOCK)
                    ON map.Setup_ID = allocation.Setup_ID
                INNER JOIN MM_AM_WorkStation AS workstn WITH (NOLOCK)
                    ON map.Station_ID = workstn.Station_ID
                WHERE allocation.Allocation_Date >= @FromDate
                  AND allocation.Allocation_Date < @ToDate
                  AND allocation.Shop_ID = @ShopId
                  AND ISNULL(allocation.Setup_ID, 0) <> 0
                  AND ISNULL(allocation.Is_Buffer_Station, 0) <> 1
                GROUP BY allocation.Line_ID, allocation.Setup_ID, allocation.Shop_ID, allocation.Shift_ID, CONVERT(DATE, allocation.Allocation_Date)
            ) AS Deploy
            LEFT JOIN
            (
                SELECT
                    directallocation.Line_ID,
                    directallocation.Setup_ID,
                    directallocation.Shop_ID,
                    directallocation.Shift_ID,
                    CONVERT(DATE, directallocation.Allocation_Date) AS Allocation_Date,
                    COUNT(DISTINCT directallocation.OSM_Direct_ID) AS DirectDeployement_Count
                FROM MM_AM_Operator_Station_Direct_Allocation AS directallocation WITH (NOLOCK)
                INNER JOIN MM_Station_Setup_Mapping AS map WITH (NOLOCK)
                    ON map.Setup_ID = directallocation.Setup_ID
                INNER JOIN MM_AM_WorkStation AS workstn WITH (NOLOCK)
                    ON map.Station_ID = workstn.Station_ID
                WHERE directallocation.Allocation_Date >= @FromDate
                  AND directallocation.Allocation_Date < @ToDate
                  AND directallocation.Shop_ID = @ShopId
                GROUP BY directallocation.Line_ID, directallocation.Setup_ID, directallocation.Shop_ID, directallocation.Shift_ID, CONVERT(DATE, directallocation.Allocation_Date)
            ) AS DirectDeployement
                ON Deploy.Line_ID = DirectDeployement.Line_ID
               AND Deploy.Setup_ID = DirectDeployement.Setup_ID
               AND Deploy.Shift_ID = DirectDeployement.Shift_ID
               AND Deploy.Shop_ID = DirectDeployement.Shop_ID
               AND Deploy.Allocation_Date = DirectDeployement.Allocation_Date
            LEFT JOIN
            (
                SELECT
                    absallocation.Line_ID,
                    absallocation.Setup_ID,
                    absallocation.Shop_ID,
                    absallocation.Shift_ID,
                    COUNT(DISTINCT absallocation.OSM_ID) AS Abs_Deployement_Count
                FROM MM_AM_Operator_Station_Allocation AS absallocation WITH (NOLOCK)
                INNER JOIN MM_Station_Setup_Mapping AS map WITH (NOLOCK)
                    ON map.Setup_ID = absallocation.Setup_ID
                INNER JOIN MM_AM_WorkStation AS workstn WITH (NOLOCK)
                    ON map.Station_ID = workstn.Station_ID
                WHERE absallocation.Allocation_Date >= @FromDate
                  AND absallocation.Allocation_Date < @ToDate
                  AND absallocation.Shop_ID = @ShopId
                  AND ISNULL(absallocation.Setup_ID, 0) <> 0
                  AND ISNULL(absallocation.Is_Buffer_Station, 0) = 1
                GROUP BY absallocation.Line_ID, absallocation.Setup_ID, absallocation.Shop_ID, absallocation.Shift_ID
            ) AS AbsDeploy
                ON Deploy.Line_ID = AbsDeploy.Line_ID
               AND Deploy.Setup_ID = AbsDeploy.Setup_ID
               AND Deploy.Shift_ID = AbsDeploy.Shift_ID
               AND Deploy.Shop_ID = AbsDeploy.Shop_ID
        ) AS Deployement
            ON Requirement.Line_ID = Deployement.Line_ID
           AND Requirement.Setup_ID = Deployement.Setup_ID
           AND Requirement.Shift_ID = Deployement.Shift_ID
           AND Requirement.Shop_ID = Deployement.Shop_ID
        INNER JOIN
        (
            SELECT
                RequiredWorkstationList.Line_ID,
                RequiredWorkstationList.Setup_ID,
                RequiredWorkstationList.Shop_ID,
                RequiredWorkstationList.Shift_ID,
                RequiredWorkstationList.RequiredWorkStation - DeployedWorkStationList.Deployement_Count AS EmptyWorkstation,
                RequiredWorkstationList.RequiredWorkStation
            FROM
            (
                SELECT
                    work.Line_ID,
                    daywise.Setup_ID,
                    work.Shop_ID,
                    daywise.Shift_ID,
                    COUNT(DISTINCT work.WorkStation_ID) AS RequiredWorkStation
                FROM MM_Stations AS str1 WITH (NOLOCK)
                INNER JOIN MM_Station_Setup_Mapping AS stM WITH (NOLOCK)
                    ON str1.Station_ID = stM.Station_ID
                INNER JOIN MM_AM_WorkStation AS work WITH (NOLOCK)
                    ON str1.Station_ID = work.Station_ID
                INNER JOIN MM_AM_StationToWorkstation_Manpower AS map WITH (NOLOCK)
                    ON work.WorkStation_ID = map.WorkStation_ID
                INNER JOIN MM_Station_Type AS types WITH (NOLOCK)
                    ON types.Station_Type_ID = str1.Station_Type_ID
                INNER JOIN MM_AM_Setup_Daywise AS daywise WITH (NOLOCK)
                    ON daywise.Setup_ID = map.Setup_ID
                WHERE ISNULL(str1.Is_Buffer_Station, 0) <> 1
                  AND work.Shop_ID = @ShopId
                  AND daywise.Setup_Date >= @FromDate
                  AND daywise.Setup_Date < @ToDate
                  AND daywise.Shop_ID = @ShopId
                  AND daywise.Is_Active = 1
                GROUP BY work.Line_ID, daywise.Setup_ID, work.Shop_ID, daywise.Shift_ID
            ) AS RequiredWorkstationList
            INNER JOIN
            (
                SELECT
                    DeployedWorkStationList.Shop_ID,
                    DeployedWorkStationList.Line_ID,
                    DeployedWorkStationList.Setup_ID,
                    DeployedWorkStationList.Shift_ID,
                    ISNULL(DirectDeployement.DirectDeployement_Count, 0) + ISNULL(DeployedWorkStationList.Deployement_Count, 0) AS Deployement_Count
                FROM
                (
                    SELECT
                        allocation.Line_ID,
                        allocation.Setup_ID,
                        allocation.Shop_ID,
                        allocation.Shift_ID,
                        COUNT(DISTINCT allocation.WorkStation_ID) AS Deployement_Count
                    FROM MM_AM_Operator_Station_Allocation AS allocation WITH (NOLOCK)
                    WHERE allocation.Allocation_Date >= @FromDate
                      AND allocation.Allocation_Date < @ToDate
                      AND allocation.Shop_ID = @ShopId
                      AND ISNULL(allocation.Setup_ID, 0) <> 0
                      AND ISNULL(allocation.Is_Buffer_Station, 0) <> 1
                    GROUP BY allocation.Line_ID, allocation.Setup_ID, allocation.Shop_ID, allocation.Shift_ID
                ) AS DeployedWorkStationList
                LEFT JOIN
                (
                    SELECT
                        directallocation.Line_ID,
                        directallocation.Setup_ID,
                        directallocation.Shop_ID,
                        directallocation.Shift_ID,
                        COUNT(DISTINCT directallocation.WorkStation_ID) AS DirectDeployement_Count
                    FROM MM_AM_Operator_Station_Direct_Allocation AS directallocation WITH (NOLOCK)
                    INNER JOIN MM_Station_Setup_Mapping AS map WITH (NOLOCK)
                        ON map.Setup_ID = directallocation.Setup_ID
                    INNER JOIN MM_AM_WorkStation AS workstn WITH (NOLOCK)
                        ON map.Station_ID = workstn.Station_ID
                    WHERE directallocation.Allocation_Date >= @FromDate
                      AND directallocation.Allocation_Date < @ToDate
                      AND directallocation.Shop_ID = @ShopId
                    GROUP BY directallocation.Line_ID, directallocation.Setup_ID, directallocation.Shop_ID, directallocation.Shift_ID
                ) AS DirectDeployement
                    ON DeployedWorkStationList.Line_ID = DirectDeployement.Line_ID
                   AND DeployedWorkStationList.Setup_ID = DirectDeployement.Setup_ID
                   AND DeployedWorkStationList.Shift_ID = DirectDeployement.Shift_ID
                   AND DeployedWorkStationList.Shop_ID = DirectDeployement.Shop_ID
            ) AS DeployedWorkStationList
                ON RequiredWorkstationList.Line_ID = DeployedWorkStationList.Line_ID
               AND RequiredWorkstationList.Setup_ID = DeployedWorkStationList.Setup_ID
               AND RequiredWorkstationList.Shift_ID = DeployedWorkStationList.Shift_ID
               AND RequiredWorkstationList.Shop_ID = DeployedWorkStationList.Shop_ID
        ) AS EmptyWorkstation
            ON Requirement.Line_ID = EmptyWorkstation.Line_ID
           AND Requirement.Setup_ID = EmptyWorkstation.Setup_ID
           AND Requirement.Shift_ID = EmptyWorkstation.Shift_ID
           AND Requirement.Shop_ID = EmptyWorkstation.Shop_ID
        WHERE Deployement.Allocation_Date >= @FromDate
          AND Deployement.Allocation_Date < @ToDate
    ) AS RowData
    INNER JOIN MM_Shops AS shop WITH (NOLOCK)
        ON shop.Shop_ID = RowData.Shop_ID
    INNER JOIN MM_Setup AS stp WITH (NOLOCK)
        ON stp.Setup_ID = RowData.Setup_ID
    INNER JOIN MM_Shift AS shif WITH (NOLOCK)
        ON shif.Shift_ID = RowData.Shift_ID
    GROUP BY shop.Shop_ID, shif.Shift_Name, CONVERT(DATE, RowData.Allocation_Date)
    ORDER BY CONVERT(DATE, RowData.Allocation_Date), shif.Shift_Name;
END;
GO


--Deleting logs older then two weeks
DELETE FROM History  WHERE CreateDate < DATEADD(day, -3, GETDATE())
GO

DELETE FROM Containers  WHERE CreateDate < DATEADD(day, -3, GETDATE())
GO

  DELETE [MtData] FROM [MtData]  INNER JOIN [MtTypes] on [MtData].[Type] = [MtTypes].[id] Where [MtTypes].[ManifestId] = 37 and [MtData].[CreatedAt] < DATEADD(day, -3, GETDATE())
GO
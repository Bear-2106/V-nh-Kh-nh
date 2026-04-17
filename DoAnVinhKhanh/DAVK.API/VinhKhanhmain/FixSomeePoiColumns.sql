IF COL_LENGTH('dbo.POIs', 'TtsTextVi') IS NULL
    ALTER TABLE dbo.POIs ADD TtsTextVi nvarchar(max) NULL;
GO

IF COL_LENGTH('dbo.POIs', 'TtsTextEn') IS NULL
    ALTER TABLE dbo.POIs ADD TtsTextEn nvarchar(max) NULL;
GO

IF COL_LENGTH('dbo.POIs', 'TtsTextZh') IS NULL
    ALTER TABLE dbo.POIs ADD TtsTextZh nvarchar(max) NULL;
GO

IF COL_LENGTH('dbo.POIs', 'TtsTextFr') IS NULL
    ALTER TABLE dbo.POIs ADD TtsTextFr nvarchar(max) NULL;
GO

IF COL_LENGTH('dbo.POIs', 'TtsTextRu') IS NULL
    ALTER TABLE dbo.POIs ADD TtsTextRu nvarchar(max) NULL;
GO

UPDATE dbo.POIs
SET TtsTextVi = COALESCE(NULLIF(TtsTextVi, ''), Description)
WHERE TtsTextVi IS NULL OR TtsTextVi = '';
GO

-- Bảng lưu dự đoán World Cup 2026 (tách biệt luồng API). Chạy một lần trên database DefaultConnection.

IF OBJECT_ID(N'dbo.Wc2026_UserPrediction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Wc2026_UserPrediction
    (
        ExternalMatchId NVARCHAR(32) NOT NULL,
        UserName        NVARCHAR(128) NOT NULL,
        HomeWin         BIT NOT NULL CONSTRAINT DF_Wc2026_UserPrediction_HomeWin DEFAULT (0),
        AwayWin         BIT NOT NULL CONSTRAINT DF_Wc2026_UserPrediction_AwayWin DEFAULT (0),
        HomeGoalPre     TINYINT NULL,
        AwayGoalPre     TINYINT NULL,
        UpdatedUtc      DATETIME2 NOT NULL CONSTRAINT DF_Wc2026_UserPrediction_Updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Wc2026_UserPrediction PRIMARY KEY CLUSTERED (ExternalMatchId, UserName)
    );

    CREATE INDEX IX_Wc2026_UserPrediction_UserName ON dbo.Wc2026_UserPrediction (UserName);
END
GO

USE [DogPlatform_IdentityDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF SCHEMA_ID(N'auth') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [auth] AUTHORIZATION [dbo];');
END;
GO

IF OBJECT_ID(N'[auth].[ErrorLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [auth].[ErrorLogs]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT [PK_ErrorLogs] PRIMARY KEY,
        [OccurredAtUtc] datetime2(7) NOT NULL,
        [ServiceName] nvarchar(100) NOT NULL,
        [HttpMethod] nvarchar(10) NULL,
        [Path] nvarchar(500) NULL,
        [QueryString] nvarchar(max) NULL,
        [RequestBody] nvarchar(max) NULL,
        [StatusCode] int NULL,
        [ExceptionType] nvarchar(500) NULL,
        [Message] nvarchar(max) NULL,
        [StackTrace] nvarchar(max) NULL,
        [UserId] nvarchar(100) NULL,
        [TraceId] nvarchar(100) NULL
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[auth].[ErrorLogs]')
      AND [name] = N'IX_ErrorLogs_OccurredAtUtc'
)
BEGIN
    CREATE INDEX [IX_ErrorLogs_OccurredAtUtc]
        ON [auth].[ErrorLogs] ([OccurredAtUtc] DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[auth].[ErrorLogs]')
      AND [name] = N'IX_ErrorLogs_ServiceName'
)
BEGIN
    CREATE INDEX [IX_ErrorLogs_ServiceName]
        ON [auth].[ErrorLogs] ([ServiceName]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[auth].[ErrorLogs]')
      AND [name] = N'IX_ErrorLogs_TraceId'
)
BEGIN
    CREATE INDEX [IX_ErrorLogs_TraceId]
        ON [auth].[ErrorLogs] ([TraceId])
        WHERE [TraceId] IS NOT NULL;
END;
GO

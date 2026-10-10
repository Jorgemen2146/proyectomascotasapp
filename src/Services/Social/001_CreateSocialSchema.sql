/* DogPlatform.Social - idempotent SQL Server schema. Run manually; never auto-applied. */
USE [master];
GO

IF DB_ID(N'DogPlatform_SocialDb') IS NULL
    CREATE DATABASE [DogPlatform_SocialDb];
GO

USE [DogPlatform_SocialDb];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'social') IS NULL EXEC(N'CREATE SCHEMA [social]');
GO

IF OBJECT_ID(N'[social].[FriendRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[FriendRequests]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_FriendRequests] PRIMARY KEY,
        [RequesterUserId] uniqueidentifier NOT NULL,
        [AddresseeUserId] uniqueidentifier NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [RespondedAtUtc] datetime2 NULL,
        [PairUser1Id] AS (CASE WHEN [RequesterUserId] < [AddresseeUserId] THEN [RequesterUserId] ELSE [AddresseeUserId] END) PERSISTED,
        [PairUser2Id] AS (CASE WHEN [RequesterUserId] < [AddresseeUserId] THEN [AddresseeUserId] ELSE [RequesterUserId] END) PERSISTED,
        CONSTRAINT [CK_FriendRequests_DifferentUsers] CHECK ([RequesterUserId] <> [AddresseeUserId])
    );
END;
GO

IF COL_LENGTH(N'social.FriendRequests', N'PairUser1Id') IS NULL
    ALTER TABLE [social].[FriendRequests] ADD [PairUser1Id] AS (CASE WHEN [RequesterUserId] < [AddresseeUserId] THEN [RequesterUserId] ELSE [AddresseeUserId] END) PERSISTED;
IF COL_LENGTH(N'social.FriendRequests', N'PairUser2Id') IS NULL
    ALTER TABLE [social].[FriendRequests] ADD [PairUser2Id] AS (CASE WHEN [RequesterUserId] < [AddresseeUserId] THEN [AddresseeUserId] ELSE [RequesterUserId] END) PERSISTED;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[FriendRequests]') AND name=N'IX_FriendRequests_Users_Status')
    CREATE INDEX [IX_FriendRequests_Users_Status] ON [social].[FriendRequests]([RequesterUserId],[AddresseeUserId],[Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[FriendRequests]') AND name=N'UX_FriendRequests_PendingPair')
    CREATE UNIQUE INDEX [UX_FriendRequests_PendingPair] ON [social].[FriendRequests]([PairUser1Id],[PairUser2Id]) WHERE [Status]=N'Pending';
GO

IF OBJECT_ID(N'[social].[Friendships]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[Friendships]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Friendships] PRIMARY KEY,
        [User1Id] uniqueidentifier NOT NULL,
        [User2Id] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL
    );
END;
IF OBJECT_ID(N'[social].[CK_Friendships_Ordered]', N'C') IS NOT NULL
    ALTER TABLE [social].[Friendships] DROP CONSTRAINT [CK_Friendships_Ordered];
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[Friendships]') AND name=N'UX_Friendships_Pair')
    CREATE UNIQUE INDEX [UX_Friendships_Pair] ON [social].[Friendships]([User1Id],[User2Id]);
GO

IF OBJECT_ID(N'[social].[UserBlocks]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[UserBlocks]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_UserBlocks] PRIMARY KEY,
        [BlockerUserId] uniqueidentifier NOT NULL,
        [BlockedUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [CK_UserBlocks_DifferentUsers] CHECK ([BlockerUserId] <> [BlockedUserId])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[UserBlocks]') AND name=N'UX_UserBlocks_Pair')
    CREATE UNIQUE INDEX [UX_UserBlocks_Pair] ON [social].[UserBlocks]([BlockerUserId],[BlockedUserId]);
GO

IF OBJECT_ID(N'[social].[Conversations]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[Conversations]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Conversations] PRIMARY KEY,
        [User1Id] uniqueidentifier NOT NULL,
        [User2Id] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastMessageAtUtc] datetime2 NULL
    );
END;
IF OBJECT_ID(N'[social].[CK_Conversations_Ordered]', N'C') IS NOT NULL
    ALTER TABLE [social].[Conversations] DROP CONSTRAINT [CK_Conversations_Ordered];
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[Conversations]') AND name=N'UX_Conversations_Pair')
    CREATE UNIQUE INDEX [UX_Conversations_Pair] ON [social].[Conversations]([User1Id],[User2Id]);
GO

IF OBJECT_ID(N'[social].[ConversationParticipants]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[ConversationParticipants]
    (
        [ConversationId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [JoinedAtUtc] datetime2 NOT NULL,
        [LastReadMessageId] uniqueidentifier NULL,
        [LastReadAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY ([ConversationId],[UserId]),
        CONSTRAINT [FK_ConversationParticipants_Conversations] FOREIGN KEY ([ConversationId]) REFERENCES [social].[Conversations]([Id]) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[ConversationParticipants]') AND name=N'IX_ConversationParticipants_UserId')
    CREATE INDEX [IX_ConversationParticipants_UserId] ON [social].[ConversationParticipants]([UserId]);
GO

IF OBJECT_ID(N'[social].[Messages]', N'U') IS NULL
BEGIN
    CREATE TABLE [social].[Messages]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Messages] PRIMARY KEY,
        [ConversationId] uniqueidentifier NOT NULL,
        [SenderUserId] uniqueidentifier NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [EditedAtUtc] datetime2 NULL,
        [DeletedAtUtc] datetime2 NULL,
        CONSTRAINT [FK_Messages_Conversations] FOREIGN KEY ([ConversationId]) REFERENCES [social].[Conversations]([Id]) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[social].[Messages]') AND name=N'IX_Messages_Conversation_Cursor')
    CREATE INDEX [IX_Messages_Conversation_Cursor] ON [social].[Messages]([ConversationId],[CreatedAtUtc] DESC,[Id] DESC);
GO
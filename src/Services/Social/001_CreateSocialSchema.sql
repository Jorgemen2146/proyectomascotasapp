IF DB_ID(N'DogPlatform_SocialDb') IS NULL CREATE DATABASE [DogPlatform_SocialDb];
GO
USE [DogPlatform_SocialDb];
GO
IF SCHEMA_ID(N'social') IS NULL EXEC(N'CREATE SCHEMA [social]');
GO
CREATE TABLE [social].[FriendRequests]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[RequesterUserId] uniqueidentifier NOT NULL,[AddresseeUserId] uniqueidentifier NOT NULL,[Status] nvarchar(20) NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,[RespondedAtUtc] datetime2 NULL,CONSTRAINT [CK_FriendRequests_DifferentUsers] CHECK([RequesterUserId]<>[AddresseeUserId]));
CREATE INDEX [IX_FriendRequests_Users_Status] ON [social].[FriendRequests]([RequesterUserId],[AddresseeUserId],[Status]);
ALTER TABLE [social].[FriendRequests] ADD [PairUser1Id] AS (CASE WHEN [RequesterUserId]<[AddresseeUserId] THEN [RequesterUserId] ELSE [AddresseeUserId] END) PERSISTED,[PairUser2Id] AS (CASE WHEN [RequesterUserId]<[AddresseeUserId] THEN [AddresseeUserId] ELSE [RequesterUserId] END) PERSISTED;
CREATE UNIQUE INDEX [UX_FriendRequests_PendingPair] ON [social].[FriendRequests]([PairUser1Id],[PairUser2Id]) WHERE [Status]=N'Pending';
CREATE TABLE [social].[Friendships]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[User1Id] uniqueidentifier NOT NULL,[User2Id] uniqueidentifier NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,CONSTRAINT [CK_Friendships_Ordered] CHECK([User1Id]<[User2Id]));
CREATE UNIQUE INDEX [UX_Friendships_Pair] ON [social].[Friendships]([User1Id],[User2Id]);
CREATE TABLE [social].[UserBlocks]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[BlockerUserId] uniqueidentifier NOT NULL,[BlockedUserId] uniqueidentifier NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,CONSTRAINT [CK_UserBlocks_DifferentUsers] CHECK([BlockerUserId]<>[BlockedUserId]));
CREATE UNIQUE INDEX [UX_UserBlocks_Pair] ON [social].[UserBlocks]([BlockerUserId],[BlockedUserId]);
CREATE TABLE [social].[Conversations]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[User1Id] uniqueidentifier NOT NULL,[User2Id] uniqueidentifier NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,[LastMessageAtUtc] datetime2 NULL,CONSTRAINT [CK_Conversations_Ordered] CHECK([User1Id]<[User2Id]));
CREATE UNIQUE INDEX [UX_Conversations_Pair] ON [social].[Conversations]([User1Id],[User2Id]);
CREATE TABLE [social].[ConversationParticipants]([ConversationId] uniqueidentifier NOT NULL,[UserId] uniqueidentifier NOT NULL,[JoinedAtUtc] datetime2 NOT NULL,[LastReadMessageId] uniqueidentifier NULL,[LastReadAtUtc] datetime2 NULL,CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY([ConversationId],[UserId]),CONSTRAINT [FK_ConversationParticipants_Conversations] FOREIGN KEY([ConversationId]) REFERENCES [social].[Conversations]([Id]) ON DELETE CASCADE);
CREATE INDEX [IX_ConversationParticipants_UserId] ON [social].[ConversationParticipants]([UserId]);
CREATE TABLE [social].[Messages]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[ConversationId] uniqueidentifier NOT NULL,[SenderUserId] uniqueidentifier NOT NULL,[Content] nvarchar(2000) NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,[EditedAtUtc] datetime2 NULL,[DeletedAtUtc] datetime2 NULL,CONSTRAINT [FK_Messages_Conversations] FOREIGN KEY([ConversationId]) REFERENCES [social].[Conversations]([Id]) ON DELETE CASCADE);
CREATE INDEX [IX_Messages_Conversation_Cursor] ON [social].[Messages]([ConversationId],[CreatedAtUtc] DESC,[Id] DESC);
GO

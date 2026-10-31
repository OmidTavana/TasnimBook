SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[appuser]', N'U') IS NULL
BEGIN
    CREATE TABLE [appuser] (
        [id] int NOT NULL IDENTITY,
        [email] nvarchar(254) NOT NULL,
        [normalizedemail] nvarchar(254) NOT NULL,
        [displayname] nvarchar(80) NOT NULL,
        [passwordhash] nvarchar(max) NOT NULL,
        [createdatutc] datetime2 NOT NULL,
        [lastloginatutc] datetime2 NULL,
        [isactive] bit NOT NULL,
        CONSTRAINT [PK_appuser] PRIMARY KEY ([id])
    );
    CREATE UNIQUE INDEX [IX_appuser_normalizedemail] ON [appuser] ([normalizedemail]);
END;

IF OBJECT_ID(N'[readingprogress]', N'U') IS NULL
BEGIN
    CREATE TABLE [readingprogress] (
        [userid] int NOT NULL,
        [bookid] int NOT NULL,
        [positionpercent] decimal(5,2) NOT NULL,
        [currentpage] int NULL,
        [totalpages] int NULL,
        [updatedatutc] datetime2 NOT NULL,
        CONSTRAINT [PK_readingprogress] PRIMARY KEY ([userid], [bookid]),
        CONSTRAINT [FK_readingprogress_Books_bookid] FOREIGN KEY ([bookid]) REFERENCES [Books] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_readingprogress_appuser_userid] FOREIGN KEY ([userid]) REFERENCES [appuser] ([id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_readingprogress_bookid] ON [readingprogress] ([bookid]);
END;

IF OBJECT_ID(N'[userfavorite]', N'U') IS NULL
BEGIN
    CREATE TABLE [userfavorite] (
        [userid] int NOT NULL,
        [bookid] int NOT NULL,
        [createdatutc] datetime2 NOT NULL,
        CONSTRAINT [PK_userfavorite] PRIMARY KEY ([userid], [bookid]),
        CONSTRAINT [FK_userfavorite_Books_bookid] FOREIGN KEY ([bookid]) REFERENCES [Books] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_userfavorite_appuser_userid] FOREIGN KEY ([userid]) REFERENCES [appuser] ([id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_userfavorite_bookid] ON [userfavorite] ([bookid]);
END;

IF OBJECT_ID(N'[userrefreshtoken]', N'U') IS NULL
BEGIN
    CREATE TABLE [userrefreshtoken] (
        [id] int NOT NULL IDENTITY,
        [userid] int NOT NULL,
        [tokenhash] nvarchar(64) NOT NULL,
        [createdatutc] datetime2 NOT NULL,
        [expiresatutc] datetime2 NOT NULL,
        [revokedatutc] datetime2 NULL,
        [replacedbytokenhash] nvarchar(64) NULL,
        [devicename] nvarchar(120) NULL,
        CONSTRAINT [PK_userrefreshtoken] PRIMARY KEY ([id]),
        CONSTRAINT [FK_userrefreshtoken_appuser_userid] FOREIGN KEY ([userid]) REFERENCES [appuser] ([id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_userrefreshtoken_tokenhash] ON [userrefreshtoken] ([tokenhash]);
    CREATE INDEX [IX_userrefreshtoken_userid] ON [userrefreshtoken] ([userid]);
END;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261031143000_AddUserAccounts')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261031143000_AddUserAccounts', N'10.0.12');

COMMIT TRANSACTION;

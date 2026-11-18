IF OBJECT_ID(N'[dbo].[appnotification]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[appnotification]
    (
        [id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_appnotification] PRIMARY KEY,
        [title] NVARCHAR(120) NOT NULL,
        [message] NVARCHAR(2000) NOT NULL,
        [notificationtype] NVARCHAR(32) NOT NULL,
        [actionurl] NVARCHAR(1000) NULL,
        [versionname] NVARCHAR(32) NULL,
        [versionnumber] INT NULL,
        [platformid] INT NULL,
        [publishatutc] DATETIME2 NOT NULL,
        [expiresatutc] DATETIME2 NULL,
        [isactive] BIT NOT NULL,
        [createdatutc] DATETIME2 NOT NULL
    );

    CREATE INDEX [IX_appnotification_active_publish]
        ON [dbo].[appnotification] ([isactive], [publishatutc]);
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM [dbo].[__EFMigrationsHistory]
       WHERE [MigrationId] = N'20261118090000_AddAppNotifications'
   )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118090000_AddAppNotifications', N'10.0.12');
END;

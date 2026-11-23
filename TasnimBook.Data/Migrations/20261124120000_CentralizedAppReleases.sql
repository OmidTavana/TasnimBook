IF COL_LENGTH(N'dbo.appversion', N'contenttype') IS NULL ALTER TABLE [dbo].[appversion] ADD [contenttype] nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.appversion', N'filesize') IS NULL ALTER TABLE [dbo].[appversion] ADD [filesize] bigint NULL;
IF COL_LENGTH(N'dbo.appversion', N'isactive') IS NULL ALTER TABLE [dbo].[appversion] ADD [isactive] bit NOT NULL CONSTRAINT [DF_appversion_isactive] DEFAULT CAST(1 AS bit);
IF COL_LENGTH(N'dbo.appversion', N'ismandatory') IS NULL ALTER TABLE [dbo].[appversion] ADD [ismandatory] bit NOT NULL CONSTRAINT [DF_appversion_ismandatory] DEFAULT CAST(0 AS bit);
IF COL_LENGTH(N'dbo.appversion', N'ispublished') IS NULL ALTER TABLE [dbo].[appversion] ADD [ispublished] bit NOT NULL CONSTRAINT [DF_appversion_ispublished] DEFAULT CAST(1 AS bit);
IF COL_LENGTH(N'dbo.appversion', N'sha256') IS NULL ALTER TABLE [dbo].[appversion] ADD [sha256] nvarchar(64) NULL;
IF COL_LENGTH(N'dbo.appversion', N'storedfilename') IS NULL ALTER TABLE [dbo].[appversion] ADD [storedfilename] nvarchar(260) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_appversion_platform_release' AND object_id = OBJECT_ID(N'[dbo].[appversion]'))
    CREATE INDEX [IX_appversion_platform_release] ON [dbo].[appversion] ([platformid], [ispublished], [isactive]);

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261124120000_CentralizedAppReleases')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124120000_CentralizedAppReleases', N'10.0.12');
END;

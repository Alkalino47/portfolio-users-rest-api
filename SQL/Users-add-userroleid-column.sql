USE UserDB;
GO

IF OBJECT_ID('dbo.UserRole', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRole] (
        [UserRoleId] INT           NOT NULL,
        [UserRole]   VARCHAR (100) NOT NULL,
        CONSTRAINT [UserRole_PK] PRIMARY KEY CLUSTERED ([UserRoleId] ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserRoleId] = 1)
BEGIN
    INSERT INTO [dbo].[UserRole] ([UserRoleId], [UserRole]) VALUES
        (1, 'Admin');
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserRoleId] = 2)
BEGIN
    INSERT INTO [dbo].[UserRole] ([UserRoleId], [UserRole]) VALUES
        (2, 'User');
END;
GO

IF COL_LENGTH('dbo.Users', 'UserRoleId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [UserRoleId] INT NOT NULL
            CONSTRAINT [DF_Users_UserRoleId] DEFAULT (2);

    ALTER TABLE [dbo].[Users]
        DROP CONSTRAINT [DF_Users_UserRoleId];
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = 'FK_Users_UserRole'
        AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD CONSTRAINT [FK_Users_UserRole]
            FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRole] ([UserRoleId]);
END;
GO

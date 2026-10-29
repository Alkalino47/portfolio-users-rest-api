-- Copy to Users-table-population.local.sql and fill in your own username and Identity password hash.
USE UserDB;
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

INSERT INTO [dbo].[Users] ([UserName], [UserPass], [UserRoleId], [DateCreated], [DateModified], [IsActive]) VALUES
    ('<seed-username>', '<identity-password-hash>', 1, '2026-06-25 10:00:00.000', '2026-06-25 10:00:00.000', 1);

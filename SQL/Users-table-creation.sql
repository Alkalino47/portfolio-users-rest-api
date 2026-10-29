CREATE TABLE [dbo].[UserRole] (
    [UserRoleId] INT           NOT NULL,
    [UserRole]   VARCHAR (100) NOT NULL,
    CONSTRAINT [UserRole_PK] PRIMARY KEY CLUSTERED ([UserRoleId] ASC)
);

CREATE TABLE [dbo].[Users] (
    [UserId]       INT            IDENTITY (1, 1) NOT NULL,
    [UserName]     NVARCHAR (100) NOT NULL,
    [UserPass]     NVARCHAR (255) NOT NULL,
    [UserRoleId]   INT            NOT NULL,
    [DateCreated]  DATETIME       NOT NULL,
    [DateModified] DATETIME       NOT NULL,
    [IsActive]     BIT            NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_Users_UserRole] FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRole] ([UserRoleId]),
    CONSTRAINT [UQ_Users_UserName] UNIQUE ([UserName])
);

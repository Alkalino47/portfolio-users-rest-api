USE [UserDB];
GO

IF OBJECT_ID(N'dbo.LoginLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LoginLogs] (
        [LogId]        INT             IDENTITY (1, 1) NOT NULL,
        [Username]     NVARCHAR (100)  NOT NULL,
        [LoginTime]    DATETIME2 (7)   NOT NULL,
        [Success]      BIT             NOT NULL,
        [IpAddress]    NVARCHAR (45)   NOT NULL,
        [UserAgent]    NVARCHAR (1024) NOT NULL,
        [ErrorMessage] NVARCHAR (2000) NULL,
        CONSTRAINT [PK_LoginLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END;
GO

# .NET 10 EF Core Setup Tutorial

This tutorial keeps the original step-by-step setup notes for creating a Users REST API, updated for .NET 10, EF Core 10, SQL Server, and the current clean architecture layout used by this repository.

## Prerequisites

- Visual Studio Code, Visual Studio, Rider, or another C# editor.
- .NET 10 SDK.
- SQL Server, either installed locally or running in Docker.
- Optional: DBeaver, Azure Data Studio, SQL Server Management Studio, or another SQL client.
- Optional: Postman for endpoint testing.

Check the installed SDKs:

```console
dotnet --list-sdks
```

Trust the local HTTPS development certificate if needed:

```console
dotnet dev-certs https --trust
```

## 1. Create the Web API Project

Create a folder for the API and scaffold a new .NET 10 web API:

```console
mkdir portfolio-users-rest-api
cd portfolio-users-rest-api
dotnet new webapi --framework net10.0
```

This repository has evolved from the default template into a clean architecture-style solution. The final structure separates API, application, domain, and infrastructure concerns.

## 2. Create the Solution and Projects

Create a solution file:

```console
dotnet new sln --name portfolio-users-rest-api
```

Create the class library projects:

```console
mkdir -p src
dotnet new classlib --framework net10.0 --name Domain --output src/Domain
dotnet new classlib --framework net10.0 --name Application --output src/Application
dotnet new classlib --framework net10.0 --name Infrastructure --output src/Infrastructure
```

Add all projects to the solution:

```console
dotnet sln add portfolio-users-rest-api.csproj
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Application/Application.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
```

Add project references:

```console
dotnet add portfolio-users-rest-api.csproj reference src/Application/Application.csproj
dotnet add portfolio-users-rest-api.csproj reference src/Infrastructure/Infrastructure.csproj
dotnet add src/Application/Application.csproj reference src/Domain/Domain.csproj
dotnet add src/Infrastructure/Infrastructure.csproj reference src/Application/Application.csproj
dotnet add src/Infrastructure/Infrastructure.csproj reference src/Domain/Domain.csproj
```

The intended dependency direction is:

```text
API -> Application
API -> Infrastructure (composition root only)
Infrastructure -> Application
Infrastructure -> Domain
Application -> Domain
Domain -> no project dependencies
```

## 3. Add NuGet Packages

Add Swagger to the API project:

```console
dotnet add portfolio-users-rest-api.csproj package Swashbuckle.AspNetCore
```

Add EF Core packages to the infrastructure project:

```console
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Infrastructure/Infrastructure.csproj package Microsoft.Extensions.Identity.Core
dotnet add src/Application/Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Application/Application.csproj package Microsoft.Extensions.Logging.Abstractions
```

Keep Identity and EF Core implementations in Infrastructure. The API handles application conflict exceptions translated by `UsersDbContext`, so it does not need a direct EF Core package reference.

## 4. Create SQL Server Database and Table

If using Docker, start a SQL Server container:

```console
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<sql-password>" --name users-sql -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Connect with a SQL client:

```text
Host: localhost
Port: 1433
Database: master
User: sa
Password: <sql-password>
```

Create the database:

```sql
CREATE DATABASE UserDB;
GO
```

Create the `UserRole` reference table before creating `Users`:

```sql
USE UserDB;
GO

CREATE TABLE [dbo].[UserRole] (
    [UserRoleId] INT           NOT NULL,
    [UserRole]   VARCHAR (100) NOT NULL,
    CONSTRAINT [UserRole_PK] PRIMARY KEY CLUSTERED ([UserRoleId] ASC)
);
GO
```

Create the `Users` table:

```sql
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
GO
```

This repository also includes SQL scripts:

- `SQL/Users-table-creation.sql`
- `SQL/Users-add-userpass-column.sql`
- `SQL/Users-add-userroleid-column.sql`
- `SQL/Users-table-population.sql`

## 5. Create DB Context and Model with EF Core

Install the EF Core CLI tool if it is not already installed:

```console
dotnet tool install --global dotnet-ef
```

If it is already installed, update it:

```console
dotnet tool update --global dotnet-ef
```

Scaffold the `UsersDbContext` and `User` entity from SQL Server into the infrastructure project:

```console
dotnet ef dbcontext scaffold "Server=<sql-host>,<sql-port>;Database=UserDB;User ID=<sql-user>;Password=<sql-password>;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --project src/Infrastructure/Infrastructure.csproj --startup-project portfolio-users-rest-api.csproj --context UsersDbContext --context-dir Persistence --output-dir Persistence/Entities --namespace portfolio_users_rest_api .Infrastructure.Persistence.Entities --context-namespace portfolio_users_rest_api .Infrastructure.Persistence
```

If the database changes and you need to regenerate the EF files, add `--force`:

```console
dotnet ef dbcontext scaffold "Server=<sql-host>,<sql-port>;Database=UserDB;User ID=<sql-user>;Password=<sql-password>;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --project src/Infrastructure/Infrastructure.csproj --startup-project portfolio-users-rest-api.csproj --context UsersDbContext --context-dir Persistence --output-dir Persistence/Entities --namespace portfolio_users_rest_api .Infrastructure.Persistence.Entities --context-namespace portfolio_users_rest_api .Infrastructure.Persistence --force
```

For this repository, the final model is organized slightly differently from the raw scaffold output:

- `src/Domain/Entities/User.cs` contains the domain entity.
- `src/Infrastructure/Persistence/UsersDbContext.cs` contains the EF Core `DbContext`.
- `src/Infrastructure/Persistence/UserRepository.cs` contains persistence behavior.

After scaffolding, move or adapt the generated entity into the Domain layer if you are following this repository's clean architecture layout.

## 6. Remove Unsafe Scaffolded Configuration

EF Core scaffolding may generate an `OnConfiguring` method with a connection string inside the `DbContext`.

Do not keep connection strings in source code. Remove generated code like this:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer("...");
```

Use constructor-injected `DbContextOptions<UsersDbContext>` instead:

```csharp
public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
```

## 7. Configure the Connection String

Copy `appsettings.example.json` to the ignored `appsettings.json`, then add your own connection string to that local file or set `ConnectionStrings__UserDBConn`. Keep actual credentials out of the public template:

```json
{
  "ConnectionStrings": {
    "UserDBConn": "Server=<sql-host>,<sql-port>;Database=UserDB;User ID=<sql-user>;Password=<sql-password>;TrustServerCertificate=True;"
  }
}
```

The current project reads the connection string in `src/Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`:

```csharp
var usersConnectionString = configuration.GetConnectionString("UserDBConn");
services.AddDbContext<UsersDbContext>(options => options.UseSqlServer(usersConnectionString));
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ILoginLogRepository, LoginLogRepository>();
services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
services.AddScoped<IUserPasswordHasher, IdentityUserPasswordHasher>();
services.AddScoped<IJwtTokenService, JwtTokenService>();
```

The security interfaces belong to Application; `IdentityUserPasswordHasher` wraps ASP.NET Core Identity in Infrastructure. See the [README DI table](../README.md#dependency-injection) for the complete registrations and lifetimes.

The API project calls this extension in `Program.cs`:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

## 8. Add Application Contracts and Services

Keep business-facing contracts in the Application layer:

- `IUserService`
- `IUserRepository`
- `IUserPasswordHasher` and `IJwtTokenService`
- `UserService`
- DTOs such as `UserDTO`, `UserForPostDTO`, `UserForPutDTO`, and `UserActivePatchDTO`
- Query models such as `UserPaginationQuery`
- Common result types such as `Result` and `PaginatedResult<T>`

This lets the controller depend on use cases instead of depending directly on EF Core.

Create `src/Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` so the Application layer owns its own service registrations:

```csharp
using portfolio_users_rest_api .Application.Auth;
using portfolio_users_rest_api .Application.LoginLogs;
using portfolio_users_rest_api .Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace portfolio_users_rest_api .Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginLogService, LoginLogService>();

        return services;
    }
}
```

## 9. Add Controllers

Create `Controllers/UserController.cs`.

The current route is:

```csharp
[Route("api/v1/[controller]")]
```

Because the controller is named `UserController`, the route becomes:

```text
/api/v1/user
```

The controller delegates work to `IUserService` and returns HTTP responses through `ControllerResultExtensions`.

`UserForPostDTO` and `UserForPutDTO` accept `UserPass`, but `UserDTO` does not expose it. The application service hashes `UserPass` through `IUserPasswordHasher` before saving. Its Infrastructure implementation delegates to Identity's `PasswordHasher<User>`, so existing password hashes remain compatible.

`UserForPostDTO` and `UserForPutDTO` should include `UserRoleId`. `UserDTO` should include both `UserRoleId` and `UserRole`, where `UserRole` is the description from the reference table. The login response includes `UserRole` instead of `UserRoleId`. The API does not expose CRUD operations for the `UserRole` table.

Create `Controllers/AuthController.cs` for authentication:

```text
POST /api/auth/login
```

The login endpoint accepts `userName` and `userPass`, looks up the existing user, and verifies the password through `IUserPasswordHasher`. A valid login for an active user returns basic user information and a JWT without `UserPass`; invalid credentials or inactive accounts return `401 Unauthorized`. `AuthService` attempts login auditing once in `finally`. See the [authentication workflow diagram](../README.md#authentication-successful-and-unsuccessful-paths).

Add JWT settings to your ignored local `appsettings.json`, or supply the signing key through `Jwt__SigningKey`. Generate your own key, for example with `openssl rand -hex 32`, and keep it private:

```json
{
  "Jwt": {
    "Issuer": "portfolio-users-rest-api",
    "Audiences": [
      "portfolio-users-rest-api",
      "dotnet-todo-list-api",
      "dotnet-notes-api",
      "dotnet-todo-list-web-ui",
      "react-todo-list-web-ui"
    ],
    "SigningKey": "<your-private-random-signing-key-at-least-32-bytes>",
    "ExpiresMinutes": 60
  }
}
```

Configure JWT bearer authentication in `Program.cs`, call `app.UseAuthentication()` before `app.UseAuthorization()`, and add `[Authorize]` to `UserController`. Keep `[AllowAnonymous]` on `AuthController.Login`.

## 10. Configure Swagger and Error Handling

In `Program.cs`, register controllers, Swagger, validation problem details, exception handling, application services, and infrastructure services:

```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Enable Swagger in development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

## 11. Build and Run

Restore:

```console
dotnet restore portfolio-users-rest-api.slnx
```

Build:

```console
dotnet build portfolio-users-rest-api.slnx
```

Run:

```console
dotnet run --project portfolio-users-rest-api.csproj --launch-profile https
```

Open Swagger:

```text
https://localhost:7132/swagger
```

## 12. Test the API

Use Swagger, Postman, or another HTTP client.

Example request:

```text
GET https://localhost:7132/api/v1/user?pageNumber=2&pageSize=10
```

Example create request:

```http
POST https://localhost:7132/api/v1/user
Content-Type: application/json

{
  "userName": "new-user",
  "userPass": "<your-user-password>",
  "userRoleId": 2,
  "isActive": true
}
```

Example login request:

```http
POST https://localhost:7132/api/auth/login
Content-Type: application/json

{
  "userName": "new-user",
  "userPass": "<your-user-password>"
}
```

Use the returned `token.accessToken` as:

```http
Authorization: Bearer <jwt>
```

All `/api/v1/user` endpoints require that header.

Postman assets are available in:

- `test/postman/local.users_api_collection.json`
- `test/postman/local.users_api_environment.json`

The Postman environment uses:

```text
{{protocol}}://{{api_host}}:{{port}}/api/v1/user
```

## 13. Useful EF Core Commands

List migrations:

```console
dotnet ef migrations list --project src/Infrastructure/Infrastructure.csproj --startup-project portfolio-users-rest-api.csproj
```

Add a migration if you move to a code-first workflow:

```console
dotnet ef migrations add InitialCreate --project src/Infrastructure/Infrastructure.csproj --startup-project portfolio-users-rest-api.csproj --output-dir Persistence/Migrations
```

Apply migrations:

```console
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project portfolio-users-rest-api.csproj
```

This repository currently includes SQL scripts for table creation, so migrations are optional unless you decide to manage schema changes from EF Core.

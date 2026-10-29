using portfolio_users_rest_api;
using portfolio_users_rest_api.Application.DependencyInjection;
using portfolio_users_rest_api.Infrastructure.Auth;
using portfolio_users_rest_api.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
var jwtAudiences = jwtOptions.Audiences
    .Where(audience => !string.IsNullOrWhiteSpace(audience))
    .ToArray();
if (jwtAudiences.Length == 0)
{
  throw new InvalidOperationException("JWT configuration must include at least one audience.");
}

var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
var corsAllowedOrigins = builder.Configuration
  .GetSection("Cors:AllowedOrigins")
  .Get<string[]>() ?? [];

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
  options.AddPolicy("ReactWebUi", policy =>
  {
    policy
      .WithOrigins(corsAllowedOrigins)
      .AllowAnyMethod()
      .AllowAnyHeader();
  });
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
  options.InvalidModelStateResponseFactory = context =>
  {
    var problemDetails = new ValidationProblemDetails(context.ModelState)
    {
      Status = StatusCodes.Status400BadRequest,
      Title = "Validation error",
      Detail = "One or more validation errors occurred.",
      Instance = context.HttpContext.Request.Path
    };
    problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

    return new BadRequestObjectResult(problemDetails);
  };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "Users API",
    Version = "v1",
    Description = "A .NET 10 clean architecture REST API for managing users."
  });
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    Description = "Enter a valid JWT bearer token."
  });
  options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
  {
    [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
  });
});
builder.Services.AddProblemDetails(options =>
{
  options.CustomizeProblemDetails = context =>
  {
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
  };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidIssuer = jwtOptions.Issuer,
      ValidateAudience = true,
      ValidAudiences = jwtAudiences,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = jwtSigningKey,
      ValidateLifetime = true,
      ClockSkew = TimeSpan.FromMinutes(1)
    };
  });
builder.Services.AddAuthorization();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("ReactWebUi");

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Users API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

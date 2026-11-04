using System.Text;
using Clinic.Application;
using Clinic.Application.Common.Exceptions;
using Clinic.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Clinic.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinic.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل Access Token فقط (من دون كتابة Bearer قبله)."
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

var jwt = builder.Configuration.GetSection("JwtSettings");
var secret = jwt["SecretKey"];

if (string.IsNullOrWhiteSpace(secret) ||
    Encoding.UTF8.GetByteCount(secret) < 32)
{
    throw new InvalidOperationException(
        "Configure JwtSettings:SecretKey with a secret of at least 32 UTF-8 bytes using User Secrets or an environment variable.");
}

var issuer = jwt["Issuer"]
    ?? throw new InvalidOperationException("JwtSettings:Issuer is required.");

var audience = jwt["Audience"]
    ?? throw new InvalidOperationException("JwtSettings:Audience is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Staff",
        policy => policy.RequireRole("Admin", "Receptionist"));

    options.AddPolicy("BookAppointment",
        policy => policy.RequireRole("Admin", "Receptionist", "Patient"));

    options.AddPolicy("ChangeAppointmentStatus",
        policy => policy.RequireRole(
            "Admin", "Receptionist", "Doctor", "Patient"));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features
        .Get<IExceptionHandlerFeature>()?.Error;

    var (status, title, detail) = error switch
    {
        UnauthorizedAccessException =>
            (StatusCodes.Status401Unauthorized,
             "Unauthorized",
             "Authentication failed."),

        ForbiddenAccessException ex =>
            (StatusCodes.Status403Forbidden,
             "Forbidden",
             ex.Message),

        KeyNotFoundException ex =>
            (StatusCodes.Status404NotFound,
             "Not found",
             ex.Message),

        InvalidOperationException ex =>
            (StatusCodes.Status400BadRequest,
             "Invalid request",
             ex.Message),

        _ =>
            (StatusCodes.Status500InternalServerError,
             "Server error",
             "An unexpected error occurred.")
    };

    context.Response.StatusCode = status;

    await context.Response.WriteAsJsonAsync(
        new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

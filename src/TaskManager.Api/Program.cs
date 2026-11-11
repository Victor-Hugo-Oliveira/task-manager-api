using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TaskManager.Api.Infrastructure;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers(options =>
        // Let FluentValidation own "required" rules instead of MVC inferring them from nullability.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(o =>
    {
        // Enums only as names ("High"), never raw numbers.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        // Don't echo internal type names in malformed-JSON errors.
        o.AllowInputFormatterExceptionMessages = false;
    });

// Consistent RFC 9457 error responses.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // ProblemDetails body for bare 404/405 etc.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI at /scalar

    // Apply pending migrations automatically in development.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

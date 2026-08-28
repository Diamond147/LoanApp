
using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Presentation.Middlewares;
using System.Text.Json;
using Presentation.Configurations;


// Load the .env file into the system environment(requires DotNetEnv package)
Env.Load();


var builder = WebApplication.CreateBuilder(args);


// Configure services by delegating to configuration extension methods
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddControllersWithJsonOptions();
builder.Services.AddSwaggerDocumentation();
builder.Services.ConfigureJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);


var app = builder.Build();


app.Use(async (context, next) => {
    context.Request.EnableBuffering();
    await next();
});

// Configure the Middlewaare HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loan Application API V1");
        c.OAuthUsePkce();
    });
}
if (!app.Environment.IsDevelopment())
{
    // Enforce HSTS (HTTP Strict Transport Security) in production - redirects HTTP requests to HTTPS and prevents downgrade attacks.
    app.UseHsts();
}


app.UseHttpsRedirection();

// Global exception middleware should be early in the pipeline so it can catch unhandled exceptions.
app.UseMiddleware<GlobalExceptionMiddleware>();

//Enable CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();


// Map Health Check Endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Instant liveness check
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteHealthCheckResponse
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteHealthCheckResponse
});


app.MapControllers();

app.Run();

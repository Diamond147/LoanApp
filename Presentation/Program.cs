using Application.Services.Implementations;
using Application.Services.Interfaces;
using Infrastructure.DbContexts;
using Infrastructure.ExternalServices.Implementations;
using Infrastructure.ExternalServices.Interfaces;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Repositories.Interfaces;
using Infrastructure.Repositories.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Presentation.Filters;
using Presentation.Middlewares;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cosmosConfig = builder.Configuration.GetSection("CosmosDb"); //Using GetSection

//Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseCosmos(
        accountEndpoint: cosmosConfig["AccountEndpoint"] ?? throw new InvalidOperationException("AccountEndpoint is missing"),
        accountKey: cosmosConfig["AccountKey"] ?? throw new InvalidOperationException("AccountKey is missing"),
        databaseName: cosmosConfig["DatabaseName"] ?? throw new InvalidOperationException("DatabaseName is missing")
    );
});

builder.Services.AddHttpContextAccessor();

//// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// EXTERNAL SERVICES (Paystack)
builder.Services.AddHttpClient<IPaystackClient, PaystackClient>((serviceProvider, client) =>
{
    // HttpClient is configured here, but actual setup happens in PaystackClient constructor
    // We just need to register it with DI container
})
.ConfigureHttpClient((serviceProvider, client) =>
{
    // Additional HttpClient configuration if needed
    client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
});

// Register PaystackClient with secret key from configuration
builder.Services.AddScoped<IPaystackClient>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var secretKey = builder.Configuration["Paystack:SecretKey"]!;

    return new PaystackClient(httpClient, secretKey);
});


// Register email client with Gmail SMTP configuration.
builder.Services.AddSingleton<IEmailClient>(provider =>
{
    var smtpServer = builder.Configuration["Email:SmtpServer"]!;
    var smtpPort = int.Parse(builder.Configuration["Email:SmtpPort"]!);
    var senderEmail = builder.Configuration["Email:SenderEmail"]!;
    var senderPassword = builder.Configuration["Email:SenderPassword"]!;
    var senderName = builder.Configuration["Email:SenderName"]!;

    return new EmailClient(smtpServer, smtpPort, senderEmail, senderPassword, senderName);
});


// Configure Cross-Origin Resource Sharing (CORS).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",  // React default
            "http://localhost:4200",  // Angular default
            "http://localhost:5173"   // Vite default
        )
        .AllowAnyMethod()      // Allow GET, POST, PUT, DELETE, etc.
        .AllowAnyHeader()      // Allow any headers
        .AllowCredentials();   // Allow cookies and auth tokens
    });
});


// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Display enums as strings in JSON responses
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//Swagger OAuth2 configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Loan API",
        Version = "v1",
        Description = "Endpoints for the loan application system"
    });

    // Add the custom enum schema filter
    c.SchemaFilter<EnumSchemaFilter>();

     //Display enums as strings in Swagger UI
    c.UseInlineDefinitionsForEnums();

    var azureAd = builder.Configuration.GetSection("AzureAd");
    var scope = azureAd["Scope"];
    if (string.IsNullOrWhiteSpace(scope))
    {
        throw new InvalidOperationException("AzureAd:Scope configuration is missing or empty.");
    }

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{azureAd["Instance"]}{azureAd["TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"{azureAd["Instance"]}{azureAd["TenantId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { scope, "Access the API" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { azureAd["Scope"]! }
        }
    });
});


//configure the Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = "roles";
});
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));



// Add Authorization with roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
    policy.RequireAssertion(ctx =>
        ctx.User.Identity?.Name == "Oadesola@infinion.co"));
});
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
//});

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loan API V1");
        c.OAuthClientId(builder.Configuration["SwaggerAzureAd:ClientId"]);  
        c.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

// Global exception middleware should be early in the pipeline so it can catch unhandled exceptions.
app.UseMiddleware<GlobalExceptionMiddleware>();

//Enable CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

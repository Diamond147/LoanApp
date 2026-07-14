using Application.Services.Implementations;
using Application.Services.Interfaces.Services;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.ExternalServices;
using Infrastructure.DbContexts;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.Implementations;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Repositories.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Presentation.Filters;
using Presentation.Middlewares;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using DotNetEnv;


// Load the .env file into the system environment(requires DotNetEnv package)
Env.Load();

var builder = WebApplication.CreateBuilder(args);


//// Industry Standard: Pull the fully built string straight from Configuration
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Register DbContext for PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, b =>
        b.MigrationsAssembly("Infrastructure")));
//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//    options.UseNpgsql(connectionString);
//});


builder.Services.AddHttpContextAccessor();

// Register repositories
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
builder.Services.AddScoped<IPaystackWebhook, PaystackWebhook>();
builder.Services.AddScoped<ITokenService, TokenService>();


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

// Register PaystackClient with secret key from environment variables
builder.Services.AddScoped<IPaystackClient>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    // Read Paystack secret key from environment variable (for security)
    var secretKey = Environment.GetEnvironmentVariable("PAYSTACK_SECRET_KEY") ?? "dummy_secret_key";
        //?? throw new InvalidOperationException("PAYSTACK_SECRET_KEY environment variable is missing");

    return new PaystackClient(httpClient, secretKey);
});


// Register a no-op email client for development so DI can resolve IEmailClient.
// Replace with real email client registration for production (SendGrid, SMTP, or AzureCommunicationService).
builder.Services.AddSingleton<IEmailClient, NoOpEmailClient>();


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

//Swagger OAuth2 configuration (currently disabled - add your auth provider config here)
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

    // TODO: Add OAuth2 security definition when you have an auth provider configured
    // c.AddSecurityDefinition("oauth2", ...);
    // c.AddSecurityRequirement(...);
});


// Configure authentication with an auth provider (Azure AD, custom RSA asymmetric, Auth0, etc.)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // 1. Fetch and decode the Base64 Public Key from configuration
    var publicKeyBase64 = builder.Configuration["Jwt:RsaPublicKey"]?.Trim()
        ?? throw new InvalidOperationException("RSA Public Key is missing");

    var rsa = RSA.Create();
 
    rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // 2. Use the Asymmetric RsaSecurityKey
        IssuerSigningKey = new RsaSecurityKey(rsa),

        // 3. Strictly enforce RS256 algorithm
        ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
    };
});


builder.Services.AddAuthorization();
// Add Authorization with policy
//builder.Services.AddAuthorization(options =>
//{
//{
//    options.AddPolicy("AdminPolicy", policy =>
//    policy.RequireAssertion(ctx =>
//        ctx.User.Identity?.Name == "admin@example.com"));
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
        //c.OAuthClientId(builder.Configuration["SwaggerAzureAd:ClientId"]);  
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

using Application.Services.Implementations;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using DotNetEnv;
using Infrastructure.DbContexts;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.Implementations;
using Infrastructure.Repositories.Implementations;
using Infrastructure.Repositories.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Presentation.Filters;
using Presentation.Middlewares;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;


// Load the .env file into the system environment(requires DotNetEnv package)
Env.Load();

var builder = WebApplication.CreateBuilder(args);


// Industry Standard: Pull the fully built string straight from Configuration
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Register DbContext for PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, b =>
        b.MigrationsAssembly("Infrastructure"));

    // In development enable EF Core SQL logging and sensitive data for diagnostics only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});


builder.Services.AddHttpContextAccessor();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IPrequalifiedLoanRepo, PrequalifiedLoanRepo>();
builder.Services.AddScoped<ILoanHistoryRepository, LoanHistoryRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPaystackWebhook, PaystackWebhook>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPrequalifiedLoanService, PrequalifiedLoanService>();
builder.Services.AddScoped<ILoanHistoryService, LoanHistoryService>();



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
    var secretKey = Environment.GetEnvironmentVariable("Paystack__SecretKey")
        ?? throw new InvalidOperationException("Paystack__SecretKey environment variable is missing");

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
        Title = "Loan Application API",
        Version = "v1",
        Description = "Endpoints for the loan application system"
    });

    // Add the custom enum schema filter
    c.SchemaFilter<EnumSchemaFilter>();

    // Strip "string" defaults from all query parameters globally
    c.OperationFilter<SwaggerClearQueryParametersFilter>();

    //Display enums as strings in Swagger UI
    c.UseInlineDefinitionsForEnums();

    // Forces Swagger to ignore validation attributes when generating examples
    c.SupportNonNullableReferenceTypes();

    // Globally tells Swagger to use simple placeholders instead of fuzzing regex patterns
    c.MapType<string>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("string") });

    // Add OAuth2 security definition when you have an auth provider configured
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});


{
    // Configure JWT Authentication globally for both Dev and Prod environments, validate incoming JWTs using configured RSA public key (RS256)
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {   
        // Fetch and decode the Base64 Public Key from configuration
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

        // Use the Asymmetric RsaSecurityKey
            IssuerSigningKey = new RsaSecurityKey(rsa),

        // Strictly enforce RS256 algorithm
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },

        // Map the "role" claim to the standard ClaimTypes.Role for Role-Based Authorization (RBAC).
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Intercept the request to extract the token from a secure HttpOnly cookie
                if (context.Request.Cookies.TryGetValue("X-Access-Token", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Token validation failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
}
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loan Application API V1");
        //c.OAuthClientId(builder.Configuration["SwaggerAzureAd:ClientId"]);  
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

app.MapControllers();

app.Run();

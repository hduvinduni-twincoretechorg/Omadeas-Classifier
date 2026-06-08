using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Omadeas.Classifier.API.Middleware;
using Omadeas.Classifier.Core.Constants;
using Omadeas.Classifier.Core.Interfaces;
using Omadeas.Classifier.Core.Services;
using Omadeas.Classifier.Core.Utils;
using Omadeas.Classifier.Infrastructure.Extensions;
using Omadeas.Classifier.Infrastructure.Repositories;
using Omadeas.Classifier.Infrastructure.Utilities;
using Omadeas.SuperValidator.Lib.Extensions;
using Omadeas.SuperValidator.Lib.Schema;
using Serilog;
using System.Reflection;

namespace Omadeas.Classifier.API;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddHttpClient();

        string configPath = "ValidationRules.json";
        IConfigurationRoot validationConfig = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(configPath, optional: false, reloadOnChange: false)
                .Build();
        builder.Services.AddOptions<ValidationOptions>() // Add options binding
            .Bind(validationConfig); // Bind to the "ValidationOptions" section
        builder.Services
            .AddControllers()
            .AddJsonValidation(); // Custom extension method to add JSON validation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Classifier Service API",
                Description = "An ASP.NET Core Web API for the Omadeas Classifier Module"
            });

            // using System.Reflection;
            string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            // Bearer-token input so [Authorize] endpoints can be exercised from Swagger UI.
            OpenApiSecurityScheme jwtScheme = new()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste a JWT (without the 'Bearer ' prefix).",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });
        });

        // Get connection string for DapperWrapper
        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database connection string is not configured.");

        // Map List<string> <-> jsonb (classifier.applies_to_node_types) for Dapper.
        SqlMapper.AddTypeHandler(new JsonbStringListTypeHandler());

        // Register repositories
        builder.Services.AddScoped<IClassifierSelectionModeRepository, ClassifierSelectionModeRepository>();
        builder.Services.AddScoped<IClassifierRepository, ClassifierRepository>();
        builder.Services.AddScoped<IClassifierHistoryRepository, ClassifierHistoryRepository>();
        builder.Services.AddScoped<IClassifierValueRepository, ClassifierValueRepository>();
        builder.Services.AddScoped<IClassifierValuePolicyRepository, ClassifierValuePolicyRepository>();
        builder.Services.AddScoped<IClassifierValueConstraintRepository, ClassifierValueConstraintRepository>();
        builder.Services.AddScoped<IClassifierProfileRepository, ClassifierProfileRepository>();
        builder.Services.AddScoped<IClassifierProfileMemberRepository, ClassifierProfileMemberRepository>();
        builder.Services.AddScoped<IClassifierProfileHistoryRepository, ClassifierProfileHistoryRepository>();

        // Register services
        builder.Services.AddScoped<IClassifierSelectionModeService, ClassifierSelectionModeService>();
        builder.Services.AddScoped<IClassifierService, ClassifierService>();
        builder.Services.AddScoped<IClassifierValueService, ClassifierValueService>();
        builder.Services.AddScoped<IClassifierValuePolicyService, ClassifierValuePolicyService>();
        builder.Services.AddScoped<IClassifierValueConstraintService, ClassifierValueConstraintService>();
        builder.Services.AddScoped<IClassifierProfileService, ClassifierProfileService>();
        builder.Services.AddScoped<IClassifierProfileMemberService, ClassifierProfileMemberService>();

        // Register Dapper wrapper
        builder.Services.AddScoped<IDapperWrapper>(sp => new DapperWrapper(connectionString));

        //Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();
        AddAuthentication(builder);

        builder.Services.AddCaching(builder.Configuration);
        builder.Services.AddHealthChecks();

        WebApplication app = builder.Build();

        app.MapHealthChecks("/healthcheckz");

        //Middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Configure the HTTP request pipeline
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
    }

    private static void AddAuthentication(WebApplicationBuilder builder)
    {
        // 1) Add Authentication + JWT Bearer
        string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
        RsaSecurityKey rsaKey = RsaKeyLoader.LoadPublicKey(builder.Configuration);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtIssuer,
                    IssuerSigningKey = rsaKey,
                    // The Auth service emits roles in a "role" claim; map it so RequireRole works.
                    RoleClaimType = "role"
                };
            });

        // Authorization policies. Endpoints opt in via [Authorize(Policy = Policies.<Name>)];
        // controllers also carry a bare [Authorize] so a valid token is required everywhere.
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.PlatformTeam, policy =>
                policy.RequireRole(Roles.PlatformAdmin));
        });
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Omadeas.Classifier.Infrastructure.Caching.Configuration;
using Omadeas.Classifier.Infrastructure.Caching.Interfaces;
using Omadeas.Classifier.Infrastructure.Caching.Services;
using StackExchange.Redis;

namespace Omadeas.Classifier.Infrastructure.Extensions;

/// <summary>
/// Service collection extensions for caching infrastructure.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds caching services to the service collection.
    /// Registers both Redis (distributed) and Memory cache implementations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Register caching configuration
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        // Add memory cache
        services.AddMemoryCache();

        // Add Redis cache
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Normalize connection string - convert semicolons to commas for StackExchange.Redis
            // This handles Cloud Run env vars where commas can't be used as they separate env vars
            var normalizedConnectionString = redisConnectionString.Replace(';', ',');

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = normalizedConnectionString;

                // Get Redis-specific options from configuration
                var cachingOptions = new CachingOptions();
                configuration.GetSection(CachingOptions.SectionName).Bind(cachingOptions);
                if (cachingOptions?.Redis != null)
                {
                    options.InstanceName = cachingOptions.Redis.InstanceName ?? "Common";
                    options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(normalizedConnectionString);
                    options.ConfigurationOptions.AbortOnConnectFail = cachingOptions.Redis.AbortOnConnectFail;
                    options.ConfigurationOptions.ConnectTimeout = cachingOptions.Redis.ConnectTimeoutMs;
                    options.ConfigurationOptions.DefaultDatabase = cachingOptions.Redis.DefaultDatabase;
                    options.ConfigurationOptions.Ssl = cachingOptions.Redis.UseSsl;

                    if (cachingOptions.Redis.UseSsl)
                    {
                        options.ConfigurationOptions.CertificateValidation += (sender, certificate, chain, errors) =>
                        {
                            return cachingOptions.Redis.SslCertificateValidation?.ToLowerInvariant() switch
                            {
                                "none" => true,
                                "remote" => errors == System.Net.Security.SslPolicyErrors.None,
                                _ => errors == System.Net.Security.SslPolicyErrors.None
                            };
                        };
                        options.ConfigurationOptions.CheckCertificateRevocation = cachingOptions.Redis.CheckCertificateRevocation;
                    }
                }
                else
                {
                    options.InstanceName = "Common";
                }
            });

            // Register the connection multiplexer for direct Redis access
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var cachingOptions = new CachingOptions();
                configuration.GetSection(CachingOptions.SectionName).Bind(cachingOptions);

                var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(normalizedConnectionString);
                if (cachingOptions?.Redis != null)
                {
                    configOptions.AbortOnConnectFail = cachingOptions.Redis.AbortOnConnectFail;
                    configOptions.ConnectTimeout = cachingOptions.Redis.ConnectTimeoutMs;
                    configOptions.DefaultDatabase = cachingOptions.Redis.DefaultDatabase;
                    configOptions.Ssl = cachingOptions.Redis.UseSsl;

                    if (cachingOptions.Redis.UseSsl)
                    {
                        configOptions.CertificateValidation += (sender, certificate, chain, errors) =>
                        {
                            return cachingOptions.Redis.SslCertificateValidation?.ToLowerInvariant() switch
                            {
                                "none" => true,
                                "remote" => errors == System.Net.Security.SslPolicyErrors.None,
                                _ => errors == System.Net.Security.SslPolicyErrors.None
                            };
                        };
                        configOptions.CheckCertificateRevocation = cachingOptions.Redis.CheckCertificateRevocation;
                    }
                }

                return ConnectionMultiplexer.Connect(configOptions);
            });

            // Register Redis cache service as primary cache
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback to memory cache if Redis is not configured
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Adds only memory cache services to the service collection.
    /// Use this when you want to use only in-memory caching.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMemoryCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Register caching configuration
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        // Add memory cache
        services.AddMemoryCache();
        services.AddScoped<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// Adds only Redis cache services to the service collection.
    /// Use this when you want to use only distributed Redis caching.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Register caching configuration
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        // Add Redis cache
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException("Redis connection string is required when using Redis caching. Please configure 'ConnectionStrings:Redis' in your appsettings.");
        }

        // Normalize connection string - convert semicolons to commas for StackExchange.Redis
        // This handles Cloud Run env vars where commas can't be used as they separate env vars
        var normalizedConnectionString = redisConnectionString.Replace(';', ',');

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = normalizedConnectionString;

            // Get Redis-specific options from configuration
            var cachingOptions = new CachingOptions();
            configuration.GetSection(CachingOptions.SectionName).Bind(cachingOptions);
            if (cachingOptions?.Redis != null)
            {
                options.InstanceName = cachingOptions.Redis.InstanceName ?? "Common";
                options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(normalizedConnectionString);
                options.ConfigurationOptions.AbortOnConnectFail = cachingOptions.Redis.AbortOnConnectFail;
                options.ConfigurationOptions.ConnectTimeout = cachingOptions.Redis.ConnectTimeoutMs;
                options.ConfigurationOptions.DefaultDatabase = cachingOptions.Redis.DefaultDatabase;
                options.ConfigurationOptions.Ssl = cachingOptions.Redis.UseSsl;

                if (cachingOptions.Redis.UseSsl)
                {
                    options.ConfigurationOptions.CertificateValidation += (sender, certificate, chain, errors) =>
                    {
                        return cachingOptions.Redis.SslCertificateValidation?.ToLowerInvariant() switch
                        {
                            "none" => true,
                            "remote" => errors == System.Net.Security.SslPolicyErrors.None,
                            _ => errors == System.Net.Security.SslPolicyErrors.None
                        };
                    };
                    options.ConfigurationOptions.CheckCertificateRevocation = cachingOptions.Redis.CheckCertificateRevocation;
                }
            }
            else
            {
                options.InstanceName = "Common";
            }
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}

namespace Omadeas.Classifier.Infrastructure.Caching.Configuration;

/// <summary>
/// Configuration options for caching in the Classifier service.
/// </summary>
public class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Default expiry time in hours for cached items.
    /// </summary>
    public int DefaultExpiryHours { get; set; } = 24;

    /// <summary>
    /// Default expiry TimeSpan calculated from DefaultExpiryHours.
    /// </summary>
    public TimeSpan DefaultExpiry => TimeSpan.FromHours(DefaultExpiryHours);

    /// <summary>
    /// Redis-specific configuration options (optional).
    /// </summary>
    public RedisOptions? Redis { get; set; }
}

/// <summary>
/// Redis-specific configuration options.
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Redis instance name for key prefixing.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Whether to abort connection if initial connection fails.
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Response timeout in milliseconds.
    /// </summary>
    public int ResponseTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Default Redis database number.
    /// </summary>
    public int DefaultDatabase { get; set; } = 0;

    /// <summary>
    /// Whether to use SSL/TLS for Redis connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// SSL certificate validation mode.
    /// </summary>
    public string? SslCertificateValidation { get; set; }

    /// <summary>
    /// Whether to check certificate revocation.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;
}

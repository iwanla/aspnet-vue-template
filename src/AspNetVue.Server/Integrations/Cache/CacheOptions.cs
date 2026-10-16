namespace AspNetVue.Server.Integrations.Cache;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Memory (default) or Redis. Redis registers a distributed L2 behind HybridCache.</summary>
    public string Provider { get; set; } = "Memory";

    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>Applied as the Redis instance name (L2 key prefix). L1 keys stay as composed per feature.</summary>
    public string KeyPrefix { get; set; } = "aspnetvue";

    public RedisOptions Redis { get; set; } = new();

    public sealed class RedisOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
}

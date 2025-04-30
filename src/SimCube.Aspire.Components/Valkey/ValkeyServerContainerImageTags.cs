namespace SimCube.Aspire.Components.Valkey;

internal static class ValkeyServerContainerImageTags
{
    /// <remarks>valkey/valkey</remarks>
    public const string Image = "valkey/valkey";

    /// <remarks>8.0.2-alpine</remarks>
    public const string Tag = "8.1.0-alpine";

    /// <remarks>rediscommander/redis-commander</remarks>
    public const string RedisCommanderImage = "rediscommander/redis-commander";

    /// <remarks>latest</remarks>
    public const string RedisCommanderTag = "latest"; // There isn't a better tag than 'latest' which is 3 years old.

    /// <remarks>redis/redisinsight</remarks>
    public const string RedisInsightImage = "redis/redisinsight";

    /// <remarks>2.64</remarks>
    public const string RedisInsightTag = "2.68";
}

namespace SimCube.Aspire.Components.Valkey;

public static class ValkeyServerBuilderExtensions
{
    public static IResourceBuilder<ValkeyServerResource> AddValkeyServerInstance(this IDistributedApplicationBuilder builder, bool withRedisCommander = true, bool withRedisInsight = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var instance = builder
            .AddValkeyServer("valkey")
            .WithContainerName("valkey");

        if (!builder.Volatile())
        {
            instance.WithDataVolume(isReadOnly: false);
        }

        var keepRunning = builder.KeepContainersRunning();

        if (keepRunning)
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        if (withRedisCommander)
        {
            instance.WithRedisCommander(
                opt =>
                {
                    opt.WithContainerName("valkey-commander");
                    if (keepRunning)
                    {
                        opt.WithLifetime(ContainerLifetime.Persistent);
                    }
                });
        }

        if (withRedisInsight)
        {
            instance.WithRedisInsight(opt =>
            {
                opt.WithDataVolume();
                opt.WithContainerName("valkey-insight");

                if (keepRunning)
                {
                    opt.WithLifetime(ContainerLifetime.Persistent);
                }
            });
        }

        return instance;
    }

    public static IResourceBuilder<ValkeyServerResource> AddValkeyServer(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port = 6379,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var valkey = new ValkeyServerResource(name, password?.Resource);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(valkey, async (_, ct) =>
        {
            connectionString = await valkey.GetConnectionStringAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{valkey.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        builder.Services.AddHealthChecks().AddRedis(_ => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        return builder.AddResource(valkey)
                      .WithEndpoint(port: port, targetPort: 6379, name: ValkeyServerResource.PrimaryEndpointName, isProxied: false)
                      .WithImage(ValkeyServerContainerImageTags.Image, ValkeyServerContainerImageTags.Tag)
                      .WithImageRegistry(ValkeyServerContainerImageTags.Registry)
                      .WithHealthCheck(healthCheckKey)
                      .EnsureCommandLineCallback();
    }

    private static IResourceBuilder<ValkeyServerResource> EnsureCommandLineCallback(this IResourceBuilder<ValkeyServerResource> builder)
    {
        if (!builder.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out _))
        {
            builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(context =>
            {
                if (builder.Resource.PasswordParameter is { } password)
                {
                    context.Args.Add("--requirepass");
                    context.Args.Add(password);
                }

                if (builder.Resource.TryGetAnnotationsOfType<PersistenceAnnotation>(out var annotations))
                {
                    var persistenceAnnotation = annotations.Single();
                    context.Args.Add("--save");
                    context.Args.Add(
                        (persistenceAnnotation.Interval ?? TimeSpan.FromSeconds(60)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
                    context.Args.Add(persistenceAnnotation.KeysChangedThreshold.ToString(CultureInfo.InvariantCulture));
                }

                return Task.CompletedTask;
            }));
        }
        return builder;
    }

    public static IResourceBuilder<ValkeyServerResource> WithRedisCommander(this IResourceBuilder<ValkeyServerResource> builder, Action<IResourceBuilder<RedisCommanderResource>>? configureContainer = null, string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }

        containerName ??= $"{builder.Resource.Name}-commander";

        var resource = new RedisCommanderResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
            .WithImage(ValkeyServerContainerImageTags.RedisCommanderImage, ValkeyServerContainerImageTags.RedisCommanderTag)
            .WithImageRegistry(ValkeyServerContainerImageTags.RedisCommanderRegistry)
            .WithHttpEndpoint(port: 5052, targetPort: 8081, name: "http", isProxied: false)
            .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            var valkeyInstances = builder.ApplicationBuilder.Resources.OfType<ValkeyServerResource>().ToList();

            if (!valkeyInstances.Any())
            {
                // No-op if there are no valkey resources present.
                return Task.CompletedTask;
            }

            var hostsVariableBuilder = new StringBuilder();

            foreach (var valkeyInstance in valkeyInstances)
            {
                if (valkeyInstance.PrimaryEndpoint.IsAllocated)
                {
                    // Redis Commander assumes Redis is being accessed over a default Aspire container network and hardcodes the resource address
                    // This will need to be refactored once updated service discovery APIs are available
                    var hostString = $"{(hostsVariableBuilder.Length > 0 ? "," : string.Empty)}{valkeyInstance.Name}:{valkeyInstance.Name}:{valkeyInstance.PrimaryEndpoint.TargetPort}:0";
                    if (valkeyInstance.PasswordParameter is not null)
                    {
                        hostString += $":{valkeyInstance.PasswordParameter.Value}";
                    }
                    hostsVariableBuilder.Append(hostString);
                }
            }

            resourceBuilder.WithEnvironment("REDIS_HOSTS", hostsVariableBuilder.ToString());

            return Task.CompletedTask;
        });

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    public static IResourceBuilder<ValkeyServerResource> WithRedisInsight(this IResourceBuilder<ValkeyServerResource> builder, Action<IResourceBuilder<RedisInsightResource>>? configureContainer = null, string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().SingleOrDefault() is { } existingRedisCommanderResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingRedisCommanderResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }

        containerName ??= $"{builder.Resource.Name}-insight";

        var resource = new RedisInsightResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(resource)
            .WithImage(ValkeyServerContainerImageTags.RedisInsightImage, ValkeyServerContainerImageTags.RedisInsightTag)
            .WithImageRegistry(ValkeyServerContainerImageTags.RedisInsightRegistry)
            .WithHttpEndpoint(port: 5053, targetPort: 5540, name: "http", isProxied: false)
            .ExcludeFromManifest();

        // We need to wait for all endpoints to be allocated before attempting to import databases
        var endpointsAllocatedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, ct) =>
        {
            endpointsAllocatedTcs.TrySetResult();
            return Task.CompletedTask;
        });

        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(resource, async (e, ct) =>
        {
            var valkeyInstances = builder.ApplicationBuilder.Resources.OfType<ValkeyServerResource>().ToList();

            if (!valkeyInstances.Any())
            {
                // No-op if there are no Valkey resources present.
                return;
            }

            // Wait for all endpoints to be allocated before attempting to import databases
            await endpointsAllocatedTcs.Task.ConfigureAwait(false);

            var redisInsightResource = builder.ApplicationBuilder.Resources.OfType<RedisInsightResource>().Single();
            var insightEndpoint = redisInsightResource.PrimaryEndpoint;

            using var client = new HttpClient();
            client.BaseAddress = new($"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}");

            var rls = e.Services.GetRequiredService<ResourceLoggerService>();
            var resourceLogger = rls.GetLogger(resource);

            await ImportRedisDatabases(resourceLogger, valkeyInstances, client, ct).ConfigureAwait(false);
        });

        configureContainer?.Invoke(resourceBuilder);

        return builder;

        static async Task ImportRedisDatabases(ILogger resourceLogger, IEnumerable<ValkeyServerResource> valkeyInstances, HttpClient client, CancellationToken cancellationToken)
        {
            var databasesPath = "/api/databases";

            var pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 5,
            }).Build();

            await pipeline.ExecuteAsync(async (ctx) =>
            {
                await InitializeRedisInsightSettings(client, resourceLogger, ctx).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            using var stream = new MemoryStream();

            // As part of configuring RedisInsight we need to factor in the possibility that the
            // container resource is being run with persistence turned on. In this case we need
            // to get the list of existing databases because we might need to delete some.
            var lookup = await pipeline.ExecuteAsync(async (ctx) =>
            {
                var getDatabasesResponse = await client.GetFromJsonAsync<RedisDatabaseDto[]>(databasesPath, cancellationToken).ConfigureAwait(false);
                return getDatabasesResponse?.ToLookup(
                    i => i.Name ?? throw new InvalidDataException("Database name is missing."),
                    i => i.Id ?? throw new InvalidDataException("Database ID is missing."));
            }, cancellationToken).ConfigureAwait(false);

            var databasesToDelete = new List<Guid>();

            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartArray();

            foreach (var ValkeyServerResource in valkeyInstances)
            {
                if (lookup is not null && lookup.Contains(ValkeyServerResource.Name))
                {
                    // It is possible that there are multiple databases with
                    // a conflicting name so we delete them all. This just keeps
                    // track of the specific ID that we need to delete.
                    databasesToDelete.AddRange(lookup[ValkeyServerResource.Name]);
                }

                if (ValkeyServerResource.PrimaryEndpoint.IsAllocated)
                {
                    var endpoint = ValkeyServerResource.PrimaryEndpoint;
                    writer.WriteStartObject();

                    writer.WriteString("host", ValkeyServerResource.Name);
                    writer.WriteNumber("port", endpoint.TargetPort!.Value);
                    writer.WriteString("name", ValkeyServerResource.Name);
                    writer.WriteNumber("db", 0);
                    writer.WriteNull("username");
                    if (ValkeyServerResource.PasswordParameter is { } passwordParam)
                    {
                        writer.WriteString("password", passwordParam.Value);
                    }
                    else
                    {
                        writer.WriteNull("password");
                    }
                    writer.WriteString("connectionType", "STANDALONE");
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);

            var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(stream);

            content.Add(fileContent, "file", "RedisInsight_connections.json");

            var apiUrl = $"{databasesPath}/import";

            try
            {
                if (databasesToDelete.Any())
                {
                    await pipeline.ExecuteAsync(async (ctx) =>
                    {
                        // Create a DELETE request to send to the existing instance of
                        // RedisInsight with the IDs of the database to delete.
                        var deleteContent = JsonContent.Create(new
                        {
                            ids = databasesToDelete
                        });

                        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, databasesPath)
                        {
                            Content = deleteContent
                        };

                        var deleteResponse = await client.SendAsync(deleteRequest, cancellationToken).ConfigureAwait(false);
                        deleteResponse.EnsureSuccessStatusCode();

                    }, cancellationToken).ConfigureAwait(false);
                }

                await pipeline.ExecuteAsync(async (ctx) =>
                {
                    var response = await client.PostAsync(apiUrl, content, ctx)
                        .ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();
                }, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                resourceLogger.LogError("Could not import Valkey databases into RedisInsight. Reason: {Reason}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Initializes the Redis Insight settings to work around https://github.com/RedisInsight/RedisInsight/issues/3452.
    /// Redis Insight requires the encryption property to be set if the Redis database connection contains a password.
    /// </summary>
    private static async Task InitializeRedisInsightSettings(HttpClient client, ILogger resourceLogger, CancellationToken ct)
    {
        if (await AreSettingsInitialized(client, ct).ConfigureAwait(false))
        {
            return;
        }

        var jsonContent = JsonContent.Create(new
        {
            agreements = new
            {
                // all 4 are required to be set
                eula = false,
                analytics = false,
                notifications = false,
                encryption = false,
            }
        });

        var response = await client.PatchAsync("/api/settings", jsonContent, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            resourceLogger.LogDebug("Could not initialize RedisInsight settings. Reason: {reason}", await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task<bool> AreSettingsInitialized(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("/api/settings", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var jsonResponse = JsonNode.Parse(content);
        var agreements = jsonResponse?["agreements"];

        return agreements is not null;
    }

    private class RedisDatabaseDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Configures the host port that the Redis Commander resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Commander.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for RedisCommander.</returns>
    public static IResourceBuilder<RedisCommanderResource> WithHostPort(this IResourceBuilder<RedisCommanderResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Configures the host port that the Redis Insight resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Redis Insight.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for RedisInsight.</returns>
    public static IResourceBuilder<RedisInsightResource> WithHostPort(this IResourceBuilder<RedisInsightResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    public static IResourceBuilder<ValkeyServerResource> WithDataVolume(this IResourceBuilder<ValkeyServerResource> builder, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithVolume("valkey-data", "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

    public static IResourceBuilder<ValkeyServerResource> WithDataBindMount(this IResourceBuilder<ValkeyServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.WithBindMount(source, "/data", isReadOnly);
        if (!isReadOnly)
        {
            builder.WithPersistence();
        }
        return builder;
    }

    public static IResourceBuilder<ValkeyServerResource> WithPersistence(this IResourceBuilder<ValkeyServerResource> builder, TimeSpan? interval = null, long keysChangedThreshold = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new PersistenceAnnotation(interval, keysChangedThreshold), ResourceAnnotationMutationBehavior.Replace)
            .EnsureCommandLineCallback();
    }

    private sealed class PersistenceAnnotation(TimeSpan? interval, long keysChangedThreshold) : IResourceAnnotation
    {
        public TimeSpan? Interval => interval;
        public long KeysChangedThreshold => keysChangedThreshold;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Each overload targets a different resource builder type, allowing for tailored functionality. Optional volume names enhance usability, enabling users to easily provide custom names while maintaining clear and distinct method signatures.")]
    public static IResourceBuilder<RedisInsightResource> WithDataVolume(this IResourceBuilder<RedisInsightResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume("redisinsight-data", "/data");
    }

    public static IResourceBuilder<RedisInsightResource> WithDataBindMount(this IResourceBuilder<RedisInsightResource> builder, string source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/data");
    }
}

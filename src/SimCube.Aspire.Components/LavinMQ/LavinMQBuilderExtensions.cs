namespace SimCube.Aspire.Components.LavinMQ;

public static class LavinMQBuilderExtensions
{
    public static IResourceBuilder<LavinMQServerResource> AddLavinMQServerInstance(this IDistributedApplicationBuilder builder, string registry = "docker.io", string tag = LavinMQServerContainerImageTags.Tag)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var instance = builder
            .AddLavinMQServerInstance("lavinmq", registry: registry, tag: tag)
            .WithContainerName("lavinmq");

        if (builder.KeepContainersRunning())
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        if (!builder.Volatile())
        {
            instance.WithDataVolume("lavinmq-data", isReadOnly: false);
        }

        return instance;
    }

    public static IResourceBuilder<LavinMQServerResource> AddLavinMQServerInstance(this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        IResourceBuilder<ParameterResource>? virtualHost = null,
        int amqpPort = 5672,
        int managementPort = 15672,
        string registry = "docker.io",
        string tag = LavinMQServerContainerImageTags.Tag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var instance = new LavinMQServerResource(name, userName?.Resource, password?.Resource, virtualHost?.Resource);

        string? connectionString = null;

        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(instance, async (_, ct) =>
        {
            connectionString = await instance.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{instance.Name}' resource but the connection string was null.");
            }
        });

        var healthCheckKey = $"{name}_check";
        // cache the connection so it is reused on subsequent calls to the health check
        IConnection? connection = null;
        builder.Services.AddHealthChecks().AddRabbitMQ(async _ =>
        {
            // NOTE: Ensure that execution of this setup callback is deferred until after
            //       the container is built & started.
            return connection ??= await CreateConnection(connectionString!).ConfigureAwait(false);

            static Task<IConnection> CreateConnection(string connectionString)
            {
                var factory = new ConnectionFactory
                {
                    Uri = new(connectionString)
                };
                return factory.CreateConnectionAsync();
            }
        }, healthCheckKey);

        return builder.AddResource(instance)
                      .WithImage(LavinMQServerContainerImageTags.Image, tag)
                      .WithImageRegistry(registry)
                      .WithEndpoint(port: amqpPort, targetPort: 5672, name: LavinMQServerResource.PrimaryEndpointName, isProxied: false, scheme: "tcp")
                      .WithEndpoint(port: managementPort, targetPort: 15672, name: LavinMQServerResource.ManagementEndpointName, scheme: "http", isProxied: false)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables["LAVINMQ_USERNAME"] = instance.UserNameReference;
                          context.EnvironmentVariables["LAVINMQ_PASSWORD"] = instance.PasswordReference;
                          context.EnvironmentVariables["LAVINMQ_VIRUALHOST"] = instance.VirtualHostReference;
                      })
                      .WithHealthCheck(healthCheckKey)
                      .WithUrlForEndpoint(LavinMQServerResource.ManagementEndpointName, u => u.DisplayText = "LavinMQ Management UI");
    }

    public static IResourceBuilder<LavinMQServerResource> WithDataVolume(this IResourceBuilder<LavinMQServerResource> builder, string name, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name, "/var/lib/lavinmq", isReadOnly)
                      .RunWithStableNodeName();
    }

    public static IResourceBuilder<LavinMQServerResource> WithDataBindMount(this IResourceBuilder<LavinMQServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/lavinmq", isReadOnly)
                      .RunWithStableNodeName();
    }

    private static IResourceBuilder<LavinMQServerResource> RunWithStableNodeName(this IResourceBuilder<LavinMQServerResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.WithEnvironment(context =>
            {
                // Set a stable node name so queue storage is consistent between sessions
                var nodeName = $"{builder.Resource.Name}@localhost";
                context.EnvironmentVariables["LAVINMQ_NODENAME"] = nodeName;
            });
        }

        return builder;
    }

    public static Func<string> GetUsername(this IResourceBuilder<LavinMQServerResource> builder) =>
        () => builder.Resource.UserNameReference.ValueExpression;

    public static Func<string> GetPassword(this IResourceBuilder<LavinMQServerResource> builder) =>
        () => builder.Resource.PasswordReference.ValueExpression;

    public static Func<string> GetVirtualHost(this IResourceBuilder<LavinMQServerResource> builder) =>
        () => builder.Resource.VirtualHostReference?.ValueExpression ?? "/";
}

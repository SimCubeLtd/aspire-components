namespace SimCube.Aspire.Components.Azurite;

public static class AzuriteBuilderExtensions
{
    public static IResourceBuilder<AzuriteResource> AddAzuriteInstance(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var instance = builder
            .AddAzurite("azurite")
            .WithContainerName("azurite");

        if (builder.KeepContainersRunning())
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        if (!builder.Volatile())
        {
            instance.WithDataVolume("azurite-data", isReadOnly: false);
        }

        return instance;
    }

    public static IResourceBuilder<AzuriteResource> AddAzurite(this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int blobPort = 10000,
        int queuePort = 10001,
        int tablePort = 10002)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var instance = new AzuriteResource(name);

        BlobServiceClient? blobServiceClient = null;

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(instance, async (@event, ct) =>
        {
            var connectionString = await instance.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
            {
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{instance.Name}' resource but the connection string was null.");
            }

            blobServiceClient = CreateBlobServiceClient(connectionString);
        });

        var healthCheckKey = $"{instance.Name}_check";

        builder.Services.AddHealthChecks().AddAzureBlobStorage(_ => blobServiceClient ?? throw new InvalidOperationException("BlobServiceClient is not initialized."), name: healthCheckKey);

        return builder.AddResource(instance)
            .WithHealthCheck(healthCheckKey)
            .WithImage(AzuriteContainerImageTags.Image, AzuriteContainerImageTags.Tag)
            .WithImageRegistry(AzuriteContainerImageTags.Registry)
            .WithEndpoint(
                AzuriteResource.BlobEndpointName, annotation =>
                {
                    annotation.Port = blobPort;
                    annotation.TargetPort = 10000;
                    annotation.UriScheme = "http";
                    annotation.IsProxied = false;
                })
            .WithEndpoint(
                AzuriteResource.QueueEndpointName, annotation =>
                {
                    annotation.Port = queuePort;
                    annotation.TargetPort = 10001;
                    annotation.UriScheme = "http";
                    annotation.IsProxied = false;
                })

            .WithEndpoint(
                AzuriteResource.TableEndpointName, annotation =>
                {
                    annotation.Port = tablePort;
                    annotation.TargetPort = 10002;
                    annotation.UriScheme = "http";
                    annotation.IsProxied = false;
                });
    }

    public static IResourceBuilder<AzuriteResource> WithDataVolume(this IResourceBuilder<AzuriteResource> builder, string name, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name, "/data", isReadOnly);
    }

    public static IResourceBuilder<AzuriteResource> WithDataBindMount(this IResourceBuilder<AzuriteResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/data", isReadOnly);
    }

    private static BlobServiceClient CreateBlobServiceClient(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return new(uri, new DefaultAzureCredential());
        }

        return new(connectionString);
    }
}

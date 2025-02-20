namespace SimCube.Aspire.Components.MailPit;

public static class MailPitBuilderExtensions
{
    private const string MailpitDatabaseEnvVar = "MP_DATABASE";

    public static IResourceBuilder<MailPitServerResource> AddMailpitServerInstance(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var instance = builder
            .AddMailpit("mailpit")
            .WithContainerName("mailpit");

        if (builder.KeepContainersRunning())
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        if (!builder.Volatile())
        {
            instance.WithDataVolume("mailpit-data", isReadOnly: false);
        }

        return instance;
    }

    public static IResourceBuilder<MailPitServerResource> AddMailpit(this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? smtpPort = 1025,
        int? httpPort = 8025)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var instance = new MailPitServerResource(name);

        return builder.AddResource(instance)
                      .WithImage(MailpitContainerImageTags.Image, MailpitContainerImageTags.Tag)
                      .WithImageRegistry(MailpitContainerImageTags.Registry)
                      .WithEndpoint(port: smtpPort, targetPort: 1025, name: MailPitServerResource.PrimaryEndpointName, isProxied: false)
                      .WithHttpEndpoint(port: httpPort, targetPort: 8025, name: MailPitServerResource.HttpEndpointName, isProxied: false)
                      .WithHttpHealthCheck(endpointName: MailPitServerResource.HttpEndpointName)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[MailpitDatabaseEnvVar] = "/data/mailpit.db";
                      });
    }

    public static IResourceBuilder<MailPitServerResource> WithDataVolume(this IResourceBuilder<MailPitServerResource> builder, string name, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name, "/data", isReadOnly);
    }

    public static IResourceBuilder<MailPitServerResource> WithDataBindMount(this IResourceBuilder<MailPitServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/data", isReadOnly);
    }
}

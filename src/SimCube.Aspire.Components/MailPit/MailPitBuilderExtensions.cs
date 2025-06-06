using SimCube.Aspire.Components.Shared;

namespace SimCube.Aspire.Components.MailPit;

public static class MailPitBuilderExtensions
{
    private const string MailpitDatabaseEnvVar = "MP_DATABASE";

    public static IResourceBuilder<MailPitServerResource> AddMailpitServerInstance(this IDistributedApplicationBuilder builder,
        string registry = "docker.io",
        string tag = MailpitContainerImageTags.Tag,
        string containerName = "mailpit",
        string namePrefix = "")
    {
        ArgumentNullException.ThrowIfNull(builder);

        var finalContainerName = containerName.GetFinalForm(namePrefix);

        var instance = builder
            .AddMailpit(finalContainerName, registry: registry, tag: tag)
            .WithContainerName(finalContainerName);

        if (builder.KeepContainersRunning())
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        if (!builder.Volatile())
        {
            instance.WithDataVolume($"{finalContainerName}-data", isReadOnly: false);
        }

        return instance;
    }

    public static IResourceBuilder<MailPitServerResource> AddMailpit(this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? smtpPort = 1025,
        int? httpPort = 8025,
        string registry = "docker.io",
        string tag = MailpitContainerImageTags.Tag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var instance = new MailPitServerResource(name);

        return builder.AddResource(instance)
                      .WithImage(MailpitContainerImageTags.Image, tag)
                      .WithImageRegistry(registry)
                      .WithEndpoint(port: smtpPort, targetPort: 1025, name: MailPitServerResource.PrimaryEndpointName, isProxied: false)
                      .WithHttpEndpoint(port: httpPort, targetPort: 8025, name: MailPitServerResource.HttpEndpointName, isProxied: false)
                      .WithHttpHealthCheck(endpointName: MailPitServerResource.HttpEndpointName)
                      .WithEnvironment(context =>
                      {
                          context.EnvironmentVariables[MailpitDatabaseEnvVar] = "/data/mailpit.db";
                      })
                      .WithUrlForEndpoint(MailPitServerResource.HttpEndpointName, u => u.DisplayText = "Mailpit UI");
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

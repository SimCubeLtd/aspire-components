using SimCube.Aspire.Components.Shared;

namespace SimCube.Aspire.Components.PostgresServer;

public static class PostgresServerBuilderExtensions
{
    private const string DefaultPassword = "SuperSecretPassword123!";
    private const string DefaultUsername = "postgres";

    public static IResourceBuilder<PostgresServerResource> AddPostgresServerInstance(
        this IDistributedApplicationBuilder builder,
        bool withPgAdmin = true,
        string? initialization = null,
        string registry = "docker.io",
        string tag = "17.2-alpine",
        string pgAdminTag = "9.3",
        string containerName = "postgresql",
        string namePrefix = "")
    {
        var password = builder.AddParameter("postgresserver-password", DefaultPassword, secret: true);
        var username = builder.AddParameter("postgresserver-username", DefaultUsername, secret: true);

        var finalContainerName = containerName.GetFinalForm(namePrefix);

        var instance = builder.AddPostgres("postgresserver".GetFinalForm(namePrefix), userName: username, password: password, port: 5432)
            .WithEndpoint(
                "tcp", annotation =>
                {
                    annotation.Port = 5432;
                    annotation.TargetPort = 5432;
                    annotation.IsProxied = false;
                })
            .WithImageTag(tag)
            .WithImageRegistry(registry)
            .WithEnvironment("POSTGRES_PASSWORD", password)
            .WithContainerName(finalContainerName);

        if (!builder.Volatile())
        {
            instance.WithDataVolume(isReadOnly: false, name: $"{finalContainerName}-data");
        }

        var keepRunning = builder.KeepContainersRunning();

        if (initialization != null)
        {
            instance.WithBindMount(initialization, "/docker-entrypoint-initdb.d");
        }

        if (withPgAdmin)
        {
            instance.WithPgAdmin(
                options =>
                {
                    options.WithContainerName("pgadmin".GetFinalForm(namePrefix));
                    options.WithImageTag(pgAdminTag);
                    options.WithImageRegistry(registry);

                    options.WithEndpoint("http", annotation =>
                    {
                        annotation.Port = 5050;
                        annotation.TargetPort = 80;
                        annotation.IsProxied = false;
                    });

                    options.WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");

                    if (keepRunning)
                    {
                        options.WithLifetime(ContainerLifetime.Persistent);
                    }
                });
        }

        if (keepRunning)
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        return instance;
    }
}

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
        string pgAdminTag = "9.3")
    {
        var password = builder.AddParameter("postgresserver-password", DefaultPassword, secret: true);
        var username = builder.AddParameter("postgresserver-username", DefaultUsername, secret: true);

        var instance = builder.AddPostgres("postgresserver", userName: username, password: password, port: 5432)
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
            .WithContainerName("postgresql");

        if (!builder.Volatile())
        {
            instance.WithDataVolume(isReadOnly: false, name: "postgres-data");
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
                    options.WithContainerName("pgadmin");
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

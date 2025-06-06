using SimCube.Aspire.Components.Shared;

namespace SimCube.Aspire.Components.SqlServer;

public static class SqlServerBuilderExtensions
{
    private const string DefaultPassword = "SuperSecretPassword123!";

    public static IResourceBuilder<SqlServerServerResource> AddSqlServerInstance(this IDistributedApplicationBuilder builder,
        string registry = "mcr.microsoft.com",
        string tag = "2022-latest",
        string containerName = "sqlserver",
        string namePrefix = "")
    {
        var password = builder.AddParameter("sqlserver-password", DefaultPassword, secret: true);

        var finalContainerName = containerName.GetFinalForm(namePrefix);

        var instance = builder.AddSqlServer("sqlserver".GetFinalForm(namePrefix), password: password, port: 1433)
            .WithEndpoint(
                "tcp", annotation =>
                {
                    annotation.Port = 1433;
                    annotation.TargetPort = 1433;
                    annotation.IsProxied = false;
                })
            .WithEnvironment("MSSQL_SA_PASSWORD", password)
            .WithImageTag(tag)
            .WithImageRegistry(registry)
            .WithContainerName(finalContainerName);

        if (!builder.Volatile())
        {
            instance.WithDataVolume(isReadOnly: false, name: $"{finalContainerName}-data");
        }

        if (builder.KeepContainersRunning())
        {
            instance.WithLifetime(ContainerLifetime.Persistent);
        }

        return instance;
    }
}

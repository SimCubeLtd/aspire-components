namespace SimCube.Aspire.Components.Environment;

public static class EnvironmentExtensions
{
    public static bool KeepContainersRunning(this IDistributedApplicationBuilder _)
    {
        var setting = System.Environment.GetEnvironmentVariable(EnvironmentalVariables.ContainerLifetime.KeepRunning);
        return !string.IsNullOrEmpty(setting) && Convert.ToBoolean(setting);
    }

    public static bool Volatile(this IDistributedApplicationBuilder _)
    {
        var setting = System.Environment.GetEnvironmentVariable(EnvironmentalVariables.ContainerPersistence.Volatile);
        return !string.IsNullOrEmpty(setting) && Convert.ToBoolean(setting);
    }

    public static string? GetNamePrefix()
    {
        var containerName = System.Environment.GetEnvironmentVariable(EnvironmentalVariables.ContainerNaming.NamePrefix);
        if (!string.IsNullOrEmpty(containerName))
        {
            return containerName;
        }

        return null;
    }
}

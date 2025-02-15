namespace SimCube.Aspire.Components.Environment;

public static class EnvironmentExtensions
{
    public static bool KeepContainersRunning(this IDistributedApplicationBuilder builder)
    {
        var setting = System.Environment.GetEnvironmentVariable(EnvironmentalVariables.ContainerLifetime.KeepRunning);
        return !string.IsNullOrEmpty(setting) && Convert.ToBoolean(setting);
    }
}

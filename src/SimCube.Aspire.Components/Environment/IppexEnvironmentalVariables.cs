namespace SimCube.Aspire.Components.Environment;

public static class EnvironmentalVariables
{
    public const string Root = "SIMCUBE_ASPIRE_";
    public static class ContainerLifetime
    {
        public const string KeepRunning = Root + "CONTAINER_LIFETIME_KEEP_RUNNING";
    }
}

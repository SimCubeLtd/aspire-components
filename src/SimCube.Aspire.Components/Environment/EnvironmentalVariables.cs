namespace SimCube.Aspire.Components.Environment;

public static class EnvironmentalVariables
{
    public const string Root = "SIMCUBE_ASPIRE_";
    public static class ContainerLifetime
    {
        public const string KeepRunning = Root + "CONTAINER_LIFETIME_KEEP_RUNNING";
    }

    public static class ContainerPersistence
    {
        public const string Volatile = Root + "CONTAINER_PERSISTENCE_VOLATILE";
    }

    public static class ContainerNaming
    {
        public const string NamePrefix = Root + "NAME_PREFIX";
    }
}

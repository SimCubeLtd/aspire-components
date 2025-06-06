namespace SimCube.Aspire.Components;

public interface ISimCubeResourceBuilder
{
    string ContainerImage { get; }

    string ContainerNamePrefix { get; }
}

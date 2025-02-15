namespace SimCube.Aspire.Components.Azurite;

public class AzuriteResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string BlobEndpointName = "blob";
    internal const string QueueEndpointName = "queue";
    internal const string TableEndpointName = "table";

    private EndpointReference BlobEndpoint => new(this, BlobEndpointName);
    private EndpointReference QueueEndpoint => new(this, QueueEndpointName);
    private EndpointReference TableEndpoint => new(this, TableEndpointName);

    public ReferenceExpression ConnectionStringExpression =>
        AzuriteConnectionString.Create(blobEndpoint: BlobEndpoint, queueEndpoint: QueueEndpoint, tableEndpoint: TableEndpoint);
}


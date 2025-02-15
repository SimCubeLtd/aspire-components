namespace SimCube.Aspire.Components.MailPit;

public class MailPitServerResource : ContainerResource, IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "smtp";
    internal const string HttpEndpointName = "http";


    public MailPitServerResource(string name) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
    }

    public EndpointReference PrimaryEndpoint { get; }

    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{PrimaryEndpoint.Property(EndpointProperty.IPV4Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
}


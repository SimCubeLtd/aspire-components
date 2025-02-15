namespace SimCube.Aspire.Components.LavinMQ;

public class LavinMQServerResource : ContainerResource, IResourceWithConnectionString
{
    public const string PrimaryEndpointName = "tcp";
    internal const string ManagementEndpointName = "management";
    private const string DefaultUserName = "guest";
    private const string DefaultPassword = "guest";

    public LavinMQServerResource(string name, ParameterResource? userName, ParameterResource? password, ParameterResource? virtualHostParameter) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
        VirtualHostParameter = virtualHostParameter;
    }

    private EndpointReference PrimaryEndpoint { get; }

    private ParameterResource? UserNameParameter { get; }

    private ParameterResource? VirtualHostParameter { get; }

    private ParameterResource? PasswordParameter { get; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    internal ReferenceExpression? VirtualHostReference =>
        VirtualHostParameter is not null ?
            ReferenceExpression.Create($"{VirtualHostParameter}") :
            null;

    internal ReferenceExpression PasswordReference =>
        PasswordParameter is not null ?
            ReferenceExpression.Create($"{PasswordParameter}") :
            ReferenceExpression.Create($"{DefaultPassword}");

    public ReferenceExpression ConnectionStringExpression =>
        AmqpConnectionString.Create(UserNameReference, PasswordReference, VirtualHostReference, PrimaryEndpoint);
}


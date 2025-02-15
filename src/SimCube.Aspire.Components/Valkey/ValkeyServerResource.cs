namespace SimCube.Aspire.Components.Valkey;

public class ValkeyServerResource(string name, ParameterResource password)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    public ParameterResource? PasswordParameter { get; } = password;

    private ReferenceExpression BuildConnectionString()
    {
        var builder = new ReferenceExpressionBuilder();
        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.IPV4Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

        if (PasswordParameter is not null)
        {
            builder.Append($",password={PasswordParameter}");
        }

        return builder.Build();
    }

    public ReferenceExpression ConnectionStringExpression =>
        this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation) ?
            connectionStringAnnotation.Resource.ConnectionStringExpression :
            BuildConnectionString();

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default) =>
        this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation) ?
            connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken) :
            BuildConnectionString().GetValueAsync(cancellationToken);
}


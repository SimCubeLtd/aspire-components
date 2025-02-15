namespace SimCube.Aspire.Components.LavinMQ;

internal static class AmqpConnectionString
{
    public static ReferenceExpression Create(
        ReferenceExpression username,
        ReferenceExpression password,
        ReferenceExpression? virtualHost,
        EndpointReference primaryEndpoint)
    {
        var builder = new ReferenceExpressionBuilder();

        builder.Append($"amqp://{username}:{password}@{primaryEndpoint.Property(EndpointProperty.IPV4Host)}:{primaryEndpoint.Property(EndpointProperty.Port)}");

        if (virtualHost is not null)
        {
            builder.Append($"/{virtualHost}");
            return builder.Build();
        }

        builder.AppendLiteral("/");
        return builder.Build();
    }
}
using SimCube.Aspire.Components.LavinMQ;
using SimCube.Aspire.Components.MailPit;
using SimCube.Aspire.Components.PostgresServer;
using SimCube.Aspire.Components.Valkey;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddValkeyServerInstance(withRedisCommander: false, withRedisInsight: true);
builder.AddLavinMQServerInstance();
builder.AddPostgresServerInstance(withPgAdmin: true);
builder.AddMailpitServerInstance();

builder.Build().Run();

using SimCube.Aspire.Components.Valkey;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddValkeyServerInstance(withRedisCommander: true, withRedisInsight: true);

builder.Build().Run();

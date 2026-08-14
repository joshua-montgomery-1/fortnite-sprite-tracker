using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.FortniteSpriteTracker>("server");

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("sprite-tracker")))
{
    var database = builder
        .AddPostgres("postgres")
        .WithDataVolume()
        .AddDatabase("sprite-tracker");

    server
        .WithReference(database)
        .WaitFor(database);
}
else
{
    var database = builder.AddConnectionString("sprite-tracker");
    server.WithReference(database);
}

builder.Build().Run();

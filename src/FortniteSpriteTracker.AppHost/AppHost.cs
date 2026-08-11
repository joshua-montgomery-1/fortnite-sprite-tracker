var builder = DistributedApplication.CreateBuilder(args);

var database = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("sprite-tracker");

builder
    .AddProject<Projects.FortniteSpriteTracker_Server>("server")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();

using ConsensusService;
using DistributedObservationSystem.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);
builder.Services.Configure<ConsensusOptions>(builder.Configuration.GetSection(ConsensusOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

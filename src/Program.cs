using Synthient.Edge.Endpoints;
using Synthient.Edge.Utilities;

var appConfig = AppConfigLoader.Load(args);

#region Builder

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls($"http://{appConfig.Server.Host}:{appConfig.Server.Port}");

builder.Services.AddSingleton(appConfig);

#endregion

#region App

var app = builder.Build();
app.MapContextEndpoints(appConfig);

#endregion

app.Run();
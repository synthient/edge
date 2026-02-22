using Synthient.Edge.Utilities;

var appConfig = AppConfigLoader.Load(args);

#region Builder

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls($"http://{appConfig.Server.Host}:{appConfig.Server.Port}");

#endregion

var app = builder.Build();

app.Run();
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPINbuster.Application.Logging;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Desktop;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
  options.SingleLine = true;
  options.TimestampFormat = "HH:mm:ss ";
});

var connectionString = builder.Configuration.GetConnectionString("Spinbuster")
  ?? "Data Source=spinbuster.desktop.sqlite";
var settings = DesktopCompositionRoot.LoadSettings(builder.Configuration);
var documentStorageSettings = DesktopCompositionRoot.LoadDocumentStorageSettings(builder.Configuration);

DesktopCompositionRoot.ConfigureServices(builder.Services, connectionString, settings, documentStorageSettings);

using var host = builder.Build();

try
{
  using var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
  var logger = loggerFactory.CreateLogger("SPINbuster.Desktop");
  var operationId = Guid.NewGuid().ToString();

  using (logger.BeginScope(new Dictionary<string, object>
  {
    [LogProperties.OperationId] = operationId,
    [LogProperties.ApplicationUserId] = settings.CurrentUserId,
  }))
  {
    logger.LogInformation("Desktop workflow starting, operation {OperationId}", operationId);
    var result = await DocumentEngineExecutableWorkflowBootstrapper.RunAsync(host.Services);
    Console.Write(DocumentEngineExecutableWorkflowConsoleFormatter.Format(result));
    logger.LogInformation("Desktop workflow completed, operation {OperationId}", operationId);
  }
}
catch (Exception exception)
{
  Console.Error.WriteLine($"Desktop workflow failed:{Environment.NewLine}{exception}");
  Environment.ExitCode = 1;
}

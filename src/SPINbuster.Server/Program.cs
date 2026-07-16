using Microsoft.EntityFrameworkCore;
using SPINbuster.AI;
using SPINbuster.Application;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Spinbuster")
  ?? "Data Source=spinbuster.local.sqlite";

builder.Services.AddSpinbusterApplication();
builder.Services.AddSpinbusterDeterministicAi();
builder.Services.AddSpinbusterSqliteInfrastructure(connectionString);

var app = builder.Build();

app.Run();

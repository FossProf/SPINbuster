using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Spinbuster")
  ?? "Data Source=spinbuster.local.sqlite";

builder.Services.AddDbContext<SpinbusterDbContext>(options =>
  options.UseSqlite(connectionString));

var app = builder.Build();

app.Run();

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SPINbuster.Infrastructure.Persistence;

public sealed class DesignTimeSpinbusterDbContextFactory : IDesignTimeDbContextFactory<SpinbusterDbContext>
{
  internal const string DefaultConnectionString = "Data Source=spinbuster.local.sqlite";

  public SpinbusterDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<SpinbusterDbContext>();
    optionsBuilder.UseSqlite(DefaultConnectionString);
    return new SpinbusterDbContext(optionsBuilder.Options);
  }
}

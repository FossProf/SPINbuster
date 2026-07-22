using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class AddParserExecutionStatus : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "ExecutionStatus",
          table: "parser_runs",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ExecutionStatus",
          table: "parser_runs");
    }
  }
}

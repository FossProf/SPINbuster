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
          nullable: true);

      migrationBuilder.Sql(@"
        UPDATE parser_runs
        SET ExecutionStatus = CASE
          WHEN State = 2 THEN 0
          WHEN State IN (3, 4) THEN 2
          ELSE NULL
        END");
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

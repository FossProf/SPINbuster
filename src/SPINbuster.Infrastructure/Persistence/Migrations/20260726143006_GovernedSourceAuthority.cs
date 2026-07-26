using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GovernedSourceAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorityBasis",
                table: "promotion_provenances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PolicyVersion",
                table: "promotion_provenances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorityBasis",
                table: "promotion_provenances");

            migrationBuilder.DropColumn(
                name: "PolicyVersion",
                table: "promotion_provenances");
        }
    }
}

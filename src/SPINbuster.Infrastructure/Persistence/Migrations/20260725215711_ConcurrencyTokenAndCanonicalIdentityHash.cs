using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861

#nullable disable

namespace SPINbuster.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyTokenAndCanonicalIdentityHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalIdentityHash",
                table: "knowledge_documents",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_documents_ProjectId_CanonicalIdentityHash",
                table: "knowledge_documents",
                columns: new[] { "ProjectId", "CanonicalIdentityHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_knowledge_documents_ProjectId_CanonicalIdentityHash",
                table: "knowledge_documents");

            migrationBuilder.DropColumn(
                name: "CanonicalIdentityHash",
                table: "knowledge_documents");
        }
    }
}

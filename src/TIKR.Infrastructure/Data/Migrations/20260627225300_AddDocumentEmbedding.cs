using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Embedding",
                table: "Documents",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Documents");
        }
    }
}

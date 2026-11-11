using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProposedFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProposedFolder",
                table: "Documents",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedFolder",
                table: "Documents");
        }
    }
}

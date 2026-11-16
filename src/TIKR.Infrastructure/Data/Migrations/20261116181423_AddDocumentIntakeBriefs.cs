using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIntakeBriefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentIntakeBriefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    JsonPayload = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedLocally = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedGrok = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIntakeBriefs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIntakeBriefs_DocumentId",
                table: "DocumentIntakeBriefs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIntakeBriefs_DocumentId_Status",
                table: "DocumentIntakeBriefs",
                columns: new[] { "DocumentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentIntakeBriefs");
        }
    }
}

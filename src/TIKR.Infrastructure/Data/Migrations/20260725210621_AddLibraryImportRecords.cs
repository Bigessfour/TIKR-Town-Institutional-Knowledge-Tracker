using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryImportRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryImportRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryImportRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryImportRecords_DocumentId",
                table: "LibraryImportRecords",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryImportRecords_RelativePath",
                table: "LibraryImportRecords",
                column: "RelativePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryImportRecords");
        }
    }
}

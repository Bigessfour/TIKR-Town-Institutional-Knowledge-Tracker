using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementChecklistItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequirementChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequirementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "INTEGER", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    LinkedDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DocumentTemplateHint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubmitTo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementChecklistItems_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RequirementChecklistItems_Documents_LinkedDocumentId",
                        column: x => x.LinkedDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RequirementChecklistItems_Requirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "Requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementChecklistItems_ContactId",
                table: "RequirementChecklistItems",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementChecklistItems_LinkedDocumentId",
                table: "RequirementChecklistItems",
                column: "LinkedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementChecklistItems_RequirementId_SortOrder",
                table: "RequirementChecklistItems",
                columns: new[] { "RequirementId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequirementChecklistItems");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TIKR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementDueOutContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Requirements",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Requirements",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Requirements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitTo",
                table: "Requirements",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "SubmitTo",
                table: "Requirements");
        }
    }
}

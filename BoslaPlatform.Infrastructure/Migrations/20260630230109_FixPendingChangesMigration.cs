using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChangesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NationalIdImageUrl",
                table: "SpecialistVerifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NationalIdImageUrl",
                table: "SpecialistVerifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}

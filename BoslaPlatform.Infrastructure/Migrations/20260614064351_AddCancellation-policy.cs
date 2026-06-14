using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationpolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCancellation",
                table: "Specialists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CancellationNoticeHours",
                table: "Specialists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicy",
                table: "Specialists",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCancellation",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "CancellationNoticeHours",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "Specialists");
        }
    }
}

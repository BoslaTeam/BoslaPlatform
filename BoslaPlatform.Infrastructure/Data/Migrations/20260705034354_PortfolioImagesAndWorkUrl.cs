using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioImagesAndWorkUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "SpecialistPortfolioItems");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "SpecialistPortfolioItems");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "SpecialistPortfolioItems",
                newName: "CoverImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "WorkUrl",
                table: "SpecialistPortfolioItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PortfolioItemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PortfolioItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioItemImages_SpecialistPortfolioItems_PortfolioItemId",
                        column: x => x.PortfolioItemId,
                        principalTable: "SpecialistPortfolioItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItemImages_PortfolioItemId",
                table: "PortfolioItemImages",
                column: "PortfolioItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioItemImages");

            migrationBuilder.DropColumn(
                name: "WorkUrl",
                table: "SpecialistPortfolioItems");

            migrationBuilder.RenameColumn(
                name: "CoverImageUrl",
                table: "SpecialistPortfolioItems",
                newName: "MediaUrl");

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "SpecialistPortfolioItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "SpecialistPortfolioItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

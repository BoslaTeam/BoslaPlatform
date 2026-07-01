using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialistVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Specialists_AspNetUsers_VerifiedBy",
                table: "Specialists");

            migrationBuilder.DropIndex(
                name: "IX_Specialists_VerifiedBy",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "Specialists");

            migrationBuilder.CreateTable(
                name: "SpecialistVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    SpecialistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalIdImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialistVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialistVerifications_Specialists_SpecialistId",
                        column: x => x.SpecialistId,
                        principalTable: "Specialists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialistVerifications_SpecialistId",
                table: "SpecialistVerifications",
                column: "SpecialistId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialistVerifications");

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Specialists",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Specialists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedBy",
                table: "Specialists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_VerifiedBy",
                table: "Specialists",
                column: "VerifiedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Specialists_AspNetUsers_VerifiedBy",
                table: "Specialists",
                column: "VerifiedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordingCompletedAtToVideoSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecordingCompletedAt",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordingCompletedAt",
                table: "VideoSessions");
        }
    }
}

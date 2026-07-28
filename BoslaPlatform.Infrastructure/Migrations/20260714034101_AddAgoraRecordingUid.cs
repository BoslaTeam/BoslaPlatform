using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgoraRecordingUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgoraRecordingUid",
                table: "VideoSessions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgoraRecordingUid",
                table: "ScreenRecordings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgoraRecordingUid",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "AgoraRecordingUid",
                table: "ScreenRecordings");
        }
    }
}

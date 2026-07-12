using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Recordingv1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCategory",
                table: "VideoSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "VideoSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VideoSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecordingAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordingAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordingAuditLogs_VideoSessions_VideoSessionId",
                        column: x => x.VideoSessionId,
                        principalTable: "VideoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_UploadStatus_NextRetryAtUtc",
                table: "VideoSessions",
                columns: new[] { "UploadStatus", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAuditLogs_OccurredAtUtc",
                table: "RecordingAuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAuditLogs_VideoSessionId",
                table: "RecordingAuditLogs",
                column: "VideoSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecordingAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_UploadStatus_NextRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "FailureCategory",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "LastRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VideoSessions");
        }
    }
}

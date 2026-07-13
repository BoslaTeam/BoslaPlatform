using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUploadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_UploadStatus_NextRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ChecksumSha256",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "FailureCategory",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "LastRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "LastUploadAttemptUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "LastUploadError",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "UploadAttempts",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "UploadStatus",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "VideoSessions");

            migrationBuilder.RenameColumn(
                name: "UploadedAtUtc",
                table: "VideoSessions",
                newName: "S3UploadedAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "VideoSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "VideoSessions");

            migrationBuilder.RenameColumn(
                name: "S3UploadedAtUtc",
                table: "VideoSessions",
                newName: "UploadedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "ChecksumSha256",
                table: "VideoSessions",
                type: "nvarchar(64)",
                maxLength: 64,
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
                name: "LastUploadAttemptUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastUploadError",
                table: "VideoSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
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

            migrationBuilder.AddColumn<int>(
                name: "UploadAttempts",
                table: "VideoSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UploadStatus",
                table: "VideoSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "VersionId",
                table: "VideoSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_UploadStatus_NextRetryAtUtc",
                table: "VideoSessions",
                columns: new[] { "UploadStatus", "NextRetryAtUtc" });
        }
    }
}

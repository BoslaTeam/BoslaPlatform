using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BucketName",
                table: "VideoSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContentLength",
                table: "VideoSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "VideoSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastUploadError",
                table: "VideoSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "VideoSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "VideoSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BucketName",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ContentLength",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "LastUploadError",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "UploadAttempts",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "UploadStatus",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "UploadedAtUtc",
                table: "VideoSessions");
        }
    }
}

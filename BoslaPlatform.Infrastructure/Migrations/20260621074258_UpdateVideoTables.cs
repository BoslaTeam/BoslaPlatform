using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVideoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_AppointmentId",
                table: "VideoSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VideoSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Waiting",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "RecordingUrl",
                table: "VideoSessions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "VideoSessions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ScreenRecordings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ScreenRecordings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_AppointmentId",
                table: "VideoSessions",
                column: "AppointmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_AppointmentId",
                table: "VideoSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VideoSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Waiting");

            migrationBuilder.AlterColumn<string>(
                name: "RecordingUrl",
                table: "VideoSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "VideoSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ScreenRecordings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ScreenRecordings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_AppointmentId",
                table: "VideoSessions",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");
        }
    }
}

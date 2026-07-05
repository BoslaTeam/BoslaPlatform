using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecordingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScreenRecordings_Appointments_AppointmentId",
                table: "ScreenRecordings");

            migrationBuilder.DropIndex(
                name: "IX_ScreenRecordings_AppointmentId",
                table: "ScreenRecordings");

            migrationBuilder.RenameColumn(
                name: "AppointmentId",
                table: "ScreenRecordings",
                newName: "VideoSessionId");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentRecordingId",
                table: "VideoSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordingStartedAtUtc",
                table: "VideoSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordingStatus",
                table: "VideoSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScreenRecordingId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_CurrentRecordingId",
                table: "VideoSessions",
                column: "CurrentRecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreenRecordings_VideoSessionId",
                table: "ScreenRecordings",
                column: "VideoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScreenRecordingId",
                table: "Appointments",
                column: "ScreenRecordingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_ScreenRecordings_ScreenRecordingId",
                table: "Appointments",
                column: "ScreenRecordingId",
                principalTable: "ScreenRecordings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScreenRecordings_VideoSessions_VideoSessionId",
                table: "ScreenRecordings",
                column: "VideoSessionId",
                principalTable: "VideoSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoSessions_ScreenRecordings_CurrentRecordingId",
                table: "VideoSessions",
                column: "CurrentRecordingId",
                principalTable: "ScreenRecordings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_ScreenRecordings_ScreenRecordingId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScreenRecordings_VideoSessions_VideoSessionId",
                table: "ScreenRecordings");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoSessions_ScreenRecordings_CurrentRecordingId",
                table: "VideoSessions");

            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_CurrentRecordingId",
                table: "VideoSessions");

            migrationBuilder.DropIndex(
                name: "IX_ScreenRecordings_VideoSessionId",
                table: "ScreenRecordings");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScreenRecordingId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CurrentRecordingId",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "RecordingStartedAtUtc",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "RecordingStatus",
                table: "VideoSessions");

            migrationBuilder.DropColumn(
                name: "ScreenRecordingId",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "VideoSessionId",
                table: "ScreenRecordings",
                newName: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreenRecordings_AppointmentId",
                table: "ScreenRecordings",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScreenRecordings_Appointments_AppointmentId",
                table: "ScreenRecordings",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

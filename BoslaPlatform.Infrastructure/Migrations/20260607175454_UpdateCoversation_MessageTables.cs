using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoslaPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoversation_MessageTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Appointments_AppointmentId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_AppointmentId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "ConversationParticipants");

            migrationBuilder.AlterColumn<string>(
                name: "MessageText",
                table: "Messages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_CreatedAtUtc",
                table: "Messages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AppointmentId",
                table: "Conversations",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Appointments_AppointmentId",
                table: "Conversations",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Appointments_AppointmentId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_CreatedAtUtc",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_AppointmentId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "MessageText",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "Messages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Conversations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Conversations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "ConversationParticipants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_AppointmentId",
                table: "Messages",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Appointments_AppointmentId",
                table: "Messages",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}

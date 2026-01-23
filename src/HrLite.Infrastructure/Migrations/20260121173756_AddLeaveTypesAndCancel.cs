using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrLite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypesAndCancel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            // Ensure at least one valid LeaveType exists before adding FK / copying data.
            // This protects environments that already have LeaveRequests rows.
            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, "Annual", "Annual Leave", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0, null, null });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 2, "Sick", "Sick Leave", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0, null, null });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "Code", "Name", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 3, "Unpaid", "Unpaid Leave", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0, null, null });

            migrationBuilder.AddColumn<int>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LeaveTypeId",
                table: "LeaveRequests");
        }
    }
}

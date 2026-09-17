using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMeeting.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiSourceAndWeeklySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingSessions_PlantId_LineId_ShiftId_MeetingDate",
                table: "MeetingSessions");

            migrationBuilder.AddColumn<bool>(
                name: "LowerIsBetter",
                table: "KpiMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "KpiMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 1,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 2,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 3,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 4,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 5,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 6,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 7,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 8,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "SP" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 9,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 10,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 11,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 12,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "SP" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 13,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 14,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 15,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 16,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "SP" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 17,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 18,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 19,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 20,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { true, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 21,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 22,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 23,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 24,
                columns: new[] { "LowerIsBetter", "Source" },
                values: new object[] { false, "Manual" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSessions_PlantId_LineId_MeetingDate",
                table: "MeetingSessions",
                columns: new[] { "PlantId", "LineId", "MeetingDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingSessions_PlantId_LineId_MeetingDate",
                table: "MeetingSessions");

            migrationBuilder.DropColumn(
                name: "LowerIsBetter",
                table: "KpiMasters");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "KpiMasters");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSessions_PlantId_LineId_ShiftId_MeetingDate",
                table: "MeetingSessions",
                columns: new[] { "PlantId", "LineId", "ShiftId", "MeetingDate" },
                unique: true,
                filter: "[ShiftId] IS NOT NULL");
        }
    }
}

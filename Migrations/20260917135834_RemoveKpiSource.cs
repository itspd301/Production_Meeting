using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMeeting.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKpiSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "KpiMasters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "KpiMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 1,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 2,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 3,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 4,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 5,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 6,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 7,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 8,
                column: "Source",
                value: "SP");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 9,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 10,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 11,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 12,
                column: "Source",
                value: "SP");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 13,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 14,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 15,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 16,
                column: "Source",
                value: "SP");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 17,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 18,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 19,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 20,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 21,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 22,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 23,
                column: "Source",
                value: "Manual");

            migrationBuilder.UpdateData(
                table: "KpiMasters",
                keyColumn: "KpiId",
                keyValue: 24,
                column: "Source",
                value: "Manual");
        }
    }
}

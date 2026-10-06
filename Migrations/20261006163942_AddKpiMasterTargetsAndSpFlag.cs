using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMeeting.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiMasterTargetsAndSpFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "F26Value",
                table: "KpiMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "F27Value",
                table: "KpiMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSourcedFromStoredProcedure",
                table: "KpiMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StoredProcedureName",
                table: "KpiMasters",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "F26Value",
                table: "KpiMasters");

            migrationBuilder.DropColumn(
                name: "F27Value",
                table: "KpiMasters");

            migrationBuilder.DropColumn(
                name: "IsSourcedFromStoredProcedure",
                table: "KpiMasters");

            migrationBuilder.DropColumn(
                name: "StoredProcedureName",
                table: "KpiMasters");
        }
    }
}

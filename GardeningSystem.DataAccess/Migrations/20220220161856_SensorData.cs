using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GardeningSystem.DataAccess.Migrations
{
    public partial class SensorData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensordata",
                columns: table => new
                {
                    uniqueDataPointId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SoilMoisture = table.Column<double>(type: "double", nullable: false),
                    Temperature = table.Column<double>(type: "double", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "Date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensordata", x => x.uniqueDataPointId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensordata");
        }
    }
}

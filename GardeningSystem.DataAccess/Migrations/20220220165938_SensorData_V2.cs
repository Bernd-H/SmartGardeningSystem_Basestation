using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GardeningSystem.DataAccess.Migrations
{
    public partial class SensorData_V2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TimeStamp",
                table: "sensordata",
                type: "Timestamp",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TimeStamp",
                table: "sensordata",
                type: "Date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "Timestamp");
        }
    }
}

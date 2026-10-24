using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microclimate.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddSensorDeviceKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sensors_DeviceKeyHash",
                table: "Sensors",
                column: "DeviceKeyHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sensors_DeviceKeyHash",
                table: "Sensors");
        }
    }
}

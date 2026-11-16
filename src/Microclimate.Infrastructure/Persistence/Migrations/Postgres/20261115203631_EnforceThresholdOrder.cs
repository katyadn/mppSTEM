using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microclimate.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class EnforceThresholdOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Thresholds_Order",
                table: "Thresholds");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Thresholds_Order",
                table: "Thresholds",
                sql:
                    "(\"CriticalMin\" IS NOT NULL OR \"WarningMin\" "
                    + "IS NOT NULL OR \"WarningMax\" IS NOT NULL OR "
                    + "\"CriticalMax\" IS NOT NULL) AND "
                    + "(\"CriticalMin\" IS NULL OR \"WarningMin\" IS "
                    + "NULL OR \"CriticalMin\" <= \"WarningMin\") AND "
                    + "(\"WarningMin\" IS NULL OR \"WarningMax\" IS "
                    + "NULL OR \"WarningMin\" < \"WarningMax\") AND "
                    + "(\"WarningMax\" IS NULL OR \"CriticalMax\" IS "
                    + "NULL OR \"WarningMax\" <= \"CriticalMax\") AND "
                    + "(\"CriticalMin\" IS NULL OR \"WarningMax\" IS "
                    + "NULL OR \"CriticalMin\" < \"WarningMax\") AND "
                    + "(\"CriticalMin\" IS NULL OR \"CriticalMax\" IS "
                    + "NULL OR \"CriticalMin\" < \"CriticalMax\") AND "
                    + "(\"WarningMin\" IS NULL OR \"CriticalMax\" IS "
                    + "NULL OR \"WarningMin\" < \"CriticalMax\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Thresholds_Order",
                table: "Thresholds");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Thresholds_Order",
                table: "Thresholds",
                sql:
                    "(\"CriticalMin\" IS NOT NULL OR \"WarningMin\" "
                    + "IS NOT NULL OR \"WarningMax\" IS NOT NULL OR "
                    + "\"CriticalMax\" IS NOT NULL) AND "
                    + "(\"CriticalMin\" IS NULL OR \"WarningMin\" IS "
                    + "NULL OR \"CriticalMin\" <= \"WarningMin\") AND "
                    + "(\"WarningMin\" IS NULL OR \"WarningMax\" IS "
                    + "NULL OR \"WarningMin\" < \"WarningMax\") AND "
                    + "(\"WarningMax\" IS NULL OR \"CriticalMax\" IS "
                    + "NULL OR \"WarningMax\" <= \"CriticalMax\")");
        }
    }
}

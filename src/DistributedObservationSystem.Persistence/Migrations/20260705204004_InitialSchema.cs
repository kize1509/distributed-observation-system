using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedObservationSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsensusReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    WindowStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsConsensus = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsensusReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MeasuredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AlarmPriority = table.Column<int>(type: "integer", nullable: false),
                    IsConsensus = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MinimumTemperature = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaximumTemperature = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DataQuality = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastMessageAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    LastMessageId = table.Column<long>(type: "bigint", nullable: false),
                    BlockedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_SensorId_MeasuredAtUtc",
                table: "AlarmEvents",
                columns: new[] { "SensorId", "MeasuredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusReadings_WindowStartUtc",
                table: "ConsensusReadings",
                column: "WindowStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_MeasuredAtUtc",
                table: "SensorReadings",
                column: "MeasuredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId_MeasuredAtUtc",
                table: "SensorReadings",
                columns: new[] { "SensorId", "MeasuredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmEvents");

            migrationBuilder.DropTable(
                name: "ConsensusReadings");

            migrationBuilder.DropTable(
                name: "SensorReadings");

            migrationBuilder.DropTable(
                name: "Sensors");
        }
    }
}

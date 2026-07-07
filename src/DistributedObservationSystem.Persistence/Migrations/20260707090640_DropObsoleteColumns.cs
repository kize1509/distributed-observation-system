using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DistributedObservationSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropObsoleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "IsConsensus",
                table: "ConsensusReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "Sensors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConsensus",
                table: "ConsensusReadings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

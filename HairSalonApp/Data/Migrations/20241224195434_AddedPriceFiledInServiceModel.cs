using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairSalonApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedPriceFiledInServiceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Service",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Service");
        }
    }
}

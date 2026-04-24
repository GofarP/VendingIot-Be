using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingIot.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAndQuantityOnItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Harga",
                table: "Items",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Jumlah",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Harga",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Jumlah",
                table: "Items");
        }
    }
}

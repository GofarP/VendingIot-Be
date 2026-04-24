using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingIot.Migrations
{
    /// <inheritdoc />
    public partial class ChangePriceAndQuantityColumnNameInItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Jumlah",
                table: "Items",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Harga",
                table: "Items",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Items",
                newName: "Jumlah");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Items",
                newName: "Harga");
        }
    }
}

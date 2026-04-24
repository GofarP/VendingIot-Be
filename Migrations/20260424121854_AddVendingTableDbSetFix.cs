using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingIot.Migrations
{
    /// <inheritdoc />
    public partial class AddVendingTableDbSetFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendingItems_Items_ItemId",
                table: "VendingItems");

            migrationBuilder.AddForeignKey(
                name: "FK_VendingItems_Items_ItemId",
                table: "VendingItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendingItems_Items_ItemId",
                table: "VendingItems");

            migrationBuilder.AddForeignKey(
                name: "FK_VendingItems_Items_ItemId",
                table: "VendingItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

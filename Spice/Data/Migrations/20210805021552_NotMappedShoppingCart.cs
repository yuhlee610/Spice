using Microsoft.EntityFrameworkCore.Migrations;

namespace Spice.Data.Migrations
{
    public partial class NotMappedShoppingCart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCart_AspNetUsers_ApplicationUserId",
                table: "ShoppingCart");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCart_MenuItem_MenuItemId",
                table: "ShoppingCart");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCart_ApplicationUserId",
                table: "ShoppingCart");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCart_MenuItemId",
                table: "ShoppingCart");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "ShoppingCart",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "ShoppingCart",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCart_ApplicationUserId",
                table: "ShoppingCart",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCart_MenuItemId",
                table: "ShoppingCart",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCart_AspNetUsers_ApplicationUserId",
                table: "ShoppingCart",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCart_MenuItem_MenuItemId",
                table: "ShoppingCart",
                column: "MenuItemId",
                principalTable: "MenuItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

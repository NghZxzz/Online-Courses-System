using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnChuyennNganh.Migrations
{
    /// <inheritdoc />
    public partial class ThemVnPays2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "vnPays",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "vnPays",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_vnPays_UserId",
                table: "vnPays",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_vnPays_AspNetUsers_UserId",
                table: "vnPays",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vnPays_AspNetUsers_UserId",
                table: "vnPays");

            migrationBuilder.DropIndex(
                name: "IX_vnPays_UserId",
                table: "vnPays");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "vnPays");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "vnPays");
        }
    }
}

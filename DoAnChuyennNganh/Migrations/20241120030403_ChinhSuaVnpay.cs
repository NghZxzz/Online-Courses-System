using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnChuyennNganh.Migrations
{
    /// <inheritdoc />
    public partial class ChinhSuaVnpay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "vnPays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_vnPays_CourseId",
                table: "vnPays",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_vnPays_Courses_CourseId",
                table: "vnPays",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vnPays_Courses_CourseId",
                table: "vnPays");

            migrationBuilder.DropIndex(
                name: "IX_vnPays_CourseId",
                table: "vnPays");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "vnPays");
        }
    }
}

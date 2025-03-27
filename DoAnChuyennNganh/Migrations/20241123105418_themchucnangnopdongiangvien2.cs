using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnChuyennNganh.Migrations
{
    /// <inheritdoc />
    public partial class themchucnangnopdongiangvien2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certificates",
                table: "TeacherApplications");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TeacherApplications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TeacherApplications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Certificates",
                table: "TeacherApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

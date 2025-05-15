using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairSalonApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppointmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_AspNetUsers_UserId",
                table: "Appointment");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Appointment",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_AspNetUsers_UserId",
                table: "Appointment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_AspNetUsers_UserId",
                table: "Appointment");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Appointment",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_AspNetUsers_UserId",
                table: "Appointment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

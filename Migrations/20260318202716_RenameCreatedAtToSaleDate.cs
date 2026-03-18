using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_spo.Migrations
{
    /// <inheritdoc />
    public partial class RenameCreatedAtToSaleDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Sales",
                newName: "SaleDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SaleDate",
                table: "Sales",
                newName: "CreatedAt");
        }
    }
}

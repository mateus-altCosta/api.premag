using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Premag.Infrastructure.Data;

#nullable disable

namespace Premag.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260925180000_TaxaAcoUnidade")]
    public partial class TaxaAcoUnidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "taxa_aco_unidade",
                table: "frentes",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "kg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "taxa_aco_unidade",
                table: "frentes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desafio.Api.Infraestrutura.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260813120000_CompletarBeneficiarios")]
public partial class CompletarBeneficiarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ExcluidoEm",
            table: "Beneficiarios",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Beneficiarios_Cpf",
            table: "Beneficiarios",
            column: "Cpf",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Beneficiarios_Cpf", table: "Beneficiarios");
        migrationBuilder.DropColumn(name: "ExcluidoEm", table: "Beneficiarios");
    }
}

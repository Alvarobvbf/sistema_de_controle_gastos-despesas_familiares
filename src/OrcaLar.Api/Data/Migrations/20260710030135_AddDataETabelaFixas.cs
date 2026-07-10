using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaLar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataETabelaFixas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Linhas pré-existentes (criadas antes desta migration) recebem a data do dia em
            // que a migration roda, via CURRENT_DATE do próprio Postgres — não faz sentido
            // atribuir uma data arbitrária (ex.: 0001-01-01) a transações que já existiam.
            migrationBuilder.AddColumn<DateOnly>(
                name: "Data",
                table: "Transacoes",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.CreateTable(
                name: "Fixas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaDoMes = table.Column<int>(type: "integer", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fixas_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fixas_PessoaId",
                table: "Fixas",
                column: "PessoaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fixas");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "Transacoes");
        }
    }
}

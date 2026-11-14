
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatSafe.Api.Data.Migrations
{
    public partial class AlinharModeloAtual : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_Visitas_AgenteSaudeId" ON "Visitas" ("AgenteSaudeId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_Visitas_IdosoId" ON "Visitas" ("IdosoId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_Visitas_ResidenciaId" ON "Visitas" ("ResidenciaId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_RegistrosHidratacao_DispositivoId" ON "RegistrosHidratacao" ("DispositivoId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_FamiliaresIdosos_IdosoId" ON "FamiliaresIdosos" ("IdosoId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_EventosDispositivo_DispositivoId" ON "EventosDispositivo" ("DispositivoId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_Alertas_FamiliarConfirmacaoId" ON "Alertas" ("FamiliarConfirmacaoId");""");

            migrationBuilder.Sql(
                """CREATE INDEX IF NOT EXISTS "IX_Alertas_ResidenciaId" ON "Alertas" ("ResidenciaId");""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Visitas_AgenteSaudeId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Visitas_IdosoId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Visitas_ResidenciaId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_RegistrosHidratacao_DispositivoId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_FamiliaresIdosos_IdosoId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_EventosDispositivo_DispositivoId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Alertas_FamiliarConfirmacaoId";""");

            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_Alertas_ResidenciaId";""");
        }
    }
}

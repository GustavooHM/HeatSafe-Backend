using HeatSafe.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HeatSafe.Api.Data.Migrations;

[DbContext(typeof(HeatSafeDbContext))]
[Migration("202611110001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE Pessoas (
                Id INTEGER NOT NULL CONSTRAINT PK_Pessoas PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                DataCadastroUtc TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                TipoPessoa TEXT NOT NULL,
                Idade INTEGER NULL,
                PossuiVulnerabilidade INTEGER NULL,
                DescricaoVulnerabilidade TEXT NULL,
                ContatoMascarado TEXT NULL,
                UnidadeSaude TEXT NULL
            );
            CREATE TABLE Residencias (
                Id INTEGER NOT NULL CONSTRAINT PK_Residencias PRIMARY KEY AUTOINCREMENT,
                Bairro TEXT NOT NULL, Cidade TEXT NOT NULL, Estado TEXT NOT NULL,
                IdentificacaoEndereco TEXT NOT NULL, IdosoId INTEGER NOT NULL,
                UltimaMovimentacaoUtc TEXT NULL, UltimaHidratacaoUtc TEXT NULL,
                VigilanciaAteUtc TEXT NULL, Ativo INTEGER NOT NULL,
                CONSTRAINT FK_Residencias_Pessoas_IdosoId FOREIGN KEY (IdosoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_Residencias_IdosoId ON Residencias(IdosoId);

            CREATE TABLE FamiliaresIdosos (
                FamiliarId INTEGER NOT NULL, IdosoId INTEGER NOT NULL,
                CONSTRAINT PK_FamiliaresIdosos PRIMARY KEY (FamiliarId, IdosoId),
                CONSTRAINT FK_FamiliaresIdosos_Pessoas_FamiliarId FOREIGN KEY (FamiliarId) REFERENCES Pessoas(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_FamiliaresIdosos_Pessoas_IdosoId FOREIGN KEY (IdosoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT
            );

            CREATE TABLE Dispositivos (
                Id INTEGER NOT NULL CONSTRAINT PK_Dispositivos PRIMARY KEY AUTOINCREMENT,
                Codigo TEXT NOT NULL, ResidenciaId INTEGER NOT NULL, Ativo INTEGER NOT NULL,
                UltimaComunicacaoUtc TEXT NULL,
                CONSTRAINT FK_Dispositivos_Residencias_ResidenciaId FOREIGN KEY (ResidenciaId) REFERENCES Residencias(Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_Dispositivos_Codigo ON Dispositivos(Codigo);
            CREATE UNIQUE INDEX IX_Dispositivos_ResidenciaId ON Dispositivos(ResidenciaId);

            CREATE TABLE Leituras (
                Id INTEGER NOT NULL CONSTRAINT PK_Leituras PRIMARY KEY AUTOINCREMENT,
                DispositivoId INTEGER NOT NULL, TemperaturaC REAL NOT NULL, Umidade REAL NOT NULL,
                IndiceCalorInformadoC REAL NULL, NivelInformado TEXT NULL, IndiceCalorCalculadoC REAL NOT NULL,
                NivelRisco TEXT NOT NULL, MovimentoDetectado INTEGER NOT NULL,
                DataHoraUtc TEXT NOT NULL, RecebidoEmUtc TEXT NOT NULL,
                CONSTRAINT FK_Leituras_Dispositivos_DispositivoId FOREIGN KEY (DispositivoId) REFERENCES Dispositivos(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IX_Leituras_DispositivoId_DataHoraUtc ON Leituras(DispositivoId, DataHoraUtc);

            CREATE TABLE RegistrosHidratacao (
                Id INTEGER NOT NULL CONSTRAINT PK_RegistrosHidratacao PRIMARY KEY AUTOINCREMENT,
                IdosoId INTEGER NOT NULL, DispositivoId INTEGER NOT NULL, DataHoraUtc TEXT NOT NULL,
                CONSTRAINT FK_RegistrosHidratacao_Pessoas_IdosoId FOREIGN KEY (IdosoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_RegistrosHidratacao_Dispositivos_DispositivoId FOREIGN KEY (DispositivoId) REFERENCES Dispositivos(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IX_RegistrosHidratacao_IdosoId_DataHoraUtc ON RegistrosHidratacao(IdosoId, DataHoraUtc);

            CREATE TABLE EventosDispositivo (
                Id INTEGER NOT NULL CONSTRAINT PK_EventosDispositivo PRIMARY KEY AUTOINCREMENT,
                DispositivoId INTEGER NOT NULL, Tipo TEXT NOT NULL, DataHoraUtc TEXT NOT NULL, ChaveUnica TEXT NOT NULL,
                CONSTRAINT FK_EventosDispositivo_Dispositivos_DispositivoId FOREIGN KEY (DispositivoId) REFERENCES Dispositivos(Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_EventosDispositivo_ChaveUnica ON EventosDispositivo(ChaveUnica);

            CREATE TABLE Alertas (
                Id INTEGER NOT NULL CONSTRAINT PK_Alertas PRIMARY KEY AUTOINCREMENT,
                IdosoId INTEGER NOT NULL, ResidenciaId INTEGER NOT NULL, IndiceCalorC REAL NOT NULL,
                NivelRisco TEXT NOT NULL, Tipo TEXT NOT NULL, Status TEXT NOT NULL, CriadoEmUtc TEXT NOT NULL,
                ConfirmadoEmUtc TEXT NULL, FamiliarConfirmacaoId INTEGER NULL, EscalonadoEmUtc TEXT NULL,
                ResolvidoEmUtc TEXT NULL, Motivo TEXT NOT NULL,
                CONSTRAINT FK_Alertas_Pessoas_IdosoId FOREIGN KEY (IdosoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Alertas_Residencias_ResidenciaId FOREIGN KEY (ResidenciaId) REFERENCES Residencias(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Alertas_Pessoas_FamiliarConfirmacaoId FOREIGN KEY (FamiliarConfirmacaoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IX_Alertas_IdosoId_Status_Tipo ON Alertas(IdosoId, Status, Tipo);

            CREATE TABLE Visitas (
                Id INTEGER NOT NULL CONSTRAINT PK_Visitas PRIMARY KEY AUTOINCREMENT,
                IdosoId INTEGER NOT NULL, ResidenciaId INTEGER NOT NULL, AgenteSaudeId INTEGER NULL,
                AlertaId INTEGER NOT NULL, PontuacaoPrioridade REAL NOT NULL, MotivoPrioridade TEXT NOT NULL,
                Status TEXT NOT NULL, CriadaEmUtc TEXT NOT NULL, AtualizadaEmUtc TEXT NULL,
                CONSTRAINT FK_Visitas_Pessoas_IdosoId FOREIGN KEY (IdosoId) REFERENCES Pessoas(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Visitas_Residencias_ResidenciaId FOREIGN KEY (ResidenciaId) REFERENCES Residencias(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Visitas_Pessoas_AgenteSaudeId FOREIGN KEY (AgenteSaudeId) REFERENCES Pessoas(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_Visitas_Alertas_AlertaId FOREIGN KEY (AlertaId) REFERENCES Alertas(Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_Visitas_AlertaId ON Visitas(AlertaId);

            CREATE TABLE PrevisoesClimaticas (
                Id INTEGER NOT NULL CONSTRAINT PK_PrevisoesClimaticas PRIMARY KEY AUTOINCREMENT,
                DataPrevista TEXT NOT NULL, Perigoso INTEGER NOT NULL, Probabilidade REAL NOT NULL,
                ConsultadaEmUtc TEXT NOT NULL, Estacao TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IX_PrevisoesClimaticas_Estacao_DataPrevista ON PrevisoesClimaticas(Estacao, DataPrevista);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS PrevisoesClimaticas;
            DROP TABLE IF EXISTS Visitas;
            DROP TABLE IF EXISTS Alertas;
            DROP TABLE IF EXISTS EventosDispositivo;
            DROP TABLE IF EXISTS RegistrosHidratacao;
            DROP TABLE IF EXISTS Leituras;
            DROP TABLE IF EXISTS Dispositivos;
            DROP TABLE IF EXISTS FamiliaresIdosos;
            DROP TABLE IF EXISTS Residencias;
            DROP TABLE IF EXISTS Pessoas;
            """);
    }
}

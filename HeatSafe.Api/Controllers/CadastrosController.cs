using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("")]
public sealed class CadastrosController(HeatSafeDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("idosos")]
    public async Task<IActionResult> GetIdosos(CancellationToken ct) => Ok(await db.Idosos.AsNoTracking().Where(x => x.Ativo)
        .Select(x => new { x.Id, x.Nome, x.Idade, x.PossuiVulnerabilidade, residenciaId = x.Residencia == null ? (int?)null : x.Residencia.Id }).ToListAsync(ct));

    [HttpPost("idosos")]
    public async Task<IActionResult> CreateIdoso(IdosoRequest request, CancellationToken ct)
    {
        var item = new Idoso { Nome = request.Nome.Trim(), Idade = request.Idade,
            PossuiVulnerabilidade = request.PossuiVulnerabilidade,
            DescricaoVulnerabilidade = request.DescricaoVulnerabilidade?.Trim(), DataCadastroUtc = timeProvider.GetUtcNow().UtcDateTime };
        db.Idosos.Add(item);
        await db.SaveChangesAsync(ct);
        return Created($"/idosos/{item.Id}", new { item.Id, item.Nome, item.Idade });
    }

    [HttpPut("idosos/{id:int}")]
    public async Task<IActionResult> UpdateIdoso(int id, IdosoRequest request, CancellationToken ct)
    {
        var item = await db.Idosos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
        if (item is null) return NotFound();
        item.Nome = request.Nome.Trim(); item.Idade = request.Idade;
        item.PossuiVulnerabilidade = request.PossuiVulnerabilidade;
        item.DescricaoVulnerabilidade = request.DescricaoVulnerabilidade?.Trim();
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("idosos/{id:int}")]
    public async Task<IActionResult> DeactivateIdoso(int id, CancellationToken ct)
    {
        var item = await db.Idosos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.Ativo = false; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("familiares")]
    public async Task<IActionResult> GetFamiliares(CancellationToken ct) => Ok(await db.Familiares.AsNoTracking().Where(x => x.Ativo)
        .Select(x => new { x.Id, x.Nome, x.ContatoMascarado, idosos = x.IdososVinculados.Select(v => v.IdosoId) }).ToListAsync(ct));

    [HttpPost("familiares")]
    public async Task<IActionResult> CreateFamiliar(FamiliarRequest request, CancellationToken ct)
    {
        var ids = request.IdososIds.Distinct().ToArray();
        var validCount = await db.Idosos.CountAsync(x => ids.Contains(x.Id) && x.Ativo, ct);
        if (validCount != ids.Length) return BadRequest(new { mensagem = "Um ou mais idosos informados não existem ou estão inativos." });
        var item = new Familiar { Nome = request.Nome.Trim(), ContatoMascarado = request.ContatoMascarado?.Trim(), DataCadastroUtc = timeProvider.GetUtcNow().UtcDateTime };
        db.Familiares.Add(item);
        foreach (var elderId in ids) item.IdososVinculados.Add(new FamiliarIdoso { Familiar = item, IdosoId = elderId });
        await db.SaveChangesAsync(ct); return Created($"/familiares/{item.Id}", new { item.Id, item.Nome });
    }

    [HttpPut("familiares/{id:int}")]
    public async Task<IActionResult> UpdateFamiliar(int id, FamiliarRequest request, CancellationToken ct)
    {
        var item = await db.Familiares.Include(x => x.IdososVinculados).FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
        if (item is null) return NotFound();
        var ids = request.IdososIds.Distinct().ToArray();
        if (await db.Idosos.CountAsync(x => ids.Contains(x.Id) && x.Ativo, ct) != ids.Length) return BadRequest(new { mensagem = "Vínculo com idoso inválido." });
        item.Nome = request.Nome.Trim(); item.ContatoMascarado = request.ContatoMascarado?.Trim();
        db.FamiliaresIdosos.RemoveRange(item.IdososVinculados);
        item.IdososVinculados.Clear();
        foreach (var elderId in ids) item.IdososVinculados.Add(new FamiliarIdoso { FamiliarId = id, IdosoId = elderId });
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("familiares/{id:int}")]
    public async Task<IActionResult> DeactivateFamiliar(int id, CancellationToken ct)
    {
        var item = await db.Familiares.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.Ativo = false; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("agentes-saude")]
    public async Task<IActionResult> GetAgentes(CancellationToken ct) => Ok(await db.AgentesSaude.AsNoTracking().Where(x => x.Ativo)
        .Select(x => new { x.Id, x.Nome, x.UnidadeSaude }).ToListAsync(ct));

    [HttpPost("agentes-saude")]
    public async Task<IActionResult> CreateAgente(AgenteSaudeRequest request, CancellationToken ct)
    {
        var item = new AgenteSaude { Nome = request.Nome.Trim(), UnidadeSaude = request.UnidadeSaude.Trim(), DataCadastroUtc = timeProvider.GetUtcNow().UtcDateTime };
        db.AgentesSaude.Add(item); await db.SaveChangesAsync(ct); return Created($"/agentes-saude/{item.Id}", new { item.Id, item.Nome });
    }

    [HttpPut("agentes-saude/{id:int}")]
    public async Task<IActionResult> UpdateAgente(int id, AgenteSaudeRequest request, CancellationToken ct)
    {
        var item = await db.AgentesSaude.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
        if (item is null) return NotFound(); item.Nome = request.Nome.Trim(); item.UnidadeSaude = request.UnidadeSaude.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("agentes-saude/{id:int}")]
    public async Task<IActionResult> DeactivateAgente(int id, CancellationToken ct)
    {
        var item = await db.AgentesSaude.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.Ativo = false; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("residencias")]
    public async Task<IActionResult> GetResidencias(CancellationToken ct) => Ok(await db.Residencias.AsNoTracking().Include(x => x.Idoso)
        .Where(x => x.Ativo).Select(x => new { x.Id, x.Bairro, x.Cidade, x.Estado, x.IdentificacaoEndereco, x.IdosoId, idoso = x.Idoso.Nome }).ToListAsync(ct));

    [HttpPost("residencias")]
    public async Task<IActionResult> CreateResidencia(ResidenciaRequest request, CancellationToken ct)
    {
        var elder = await db.Idosos.FirstOrDefaultAsync(x => x.Id == request.IdosoId && x.Ativo, ct);
        if (elder is null) return BadRequest(new { mensagem = "Idoso não encontrado ou inativo." });
        if (await db.Residencias.AnyAsync(x => x.IdosoId == request.IdosoId, ct)) return Conflict(new { mensagem = "O idoso já possui residência cadastrada." });
        var item = new Residencia { Bairro = request.Bairro.Trim(), Cidade = request.Cidade.Trim(), Estado = request.Estado.ToUpperInvariant(),
            IdentificacaoEndereco = request.IdentificacaoEndereco.Trim(), IdosoId = request.IdosoId,
            UltimaMovimentacaoUtc = timeProvider.GetUtcNow().UtcDateTime, UltimaHidratacaoUtc = timeProvider.GetUtcNow().UtcDateTime };
        db.Residencias.Add(item); await db.SaveChangesAsync(ct); return Created($"/residencias/{item.Id}", new { item.Id, item.IdentificacaoEndereco });
    }

    [HttpPut("residencias/{id:int}")]
    public async Task<IActionResult> UpdateResidencia(int id, ResidenciaRequest request, CancellationToken ct)
    {
        var item = await db.Residencias.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
        if (item is null) return NotFound();
        if (item.IdosoId != request.IdosoId && await db.Residencias.AnyAsync(x => x.IdosoId == request.IdosoId && x.Id != id, ct)) return Conflict(new { mensagem = "O idoso já possui outra residência." });
        if (!await db.Idosos.AnyAsync(x => x.Id == request.IdosoId && x.Ativo, ct)) return BadRequest(new { mensagem = "Idoso inválido." });
        item.Bairro = request.Bairro.Trim(); item.Cidade = request.Cidade.Trim(); item.Estado = request.Estado.ToUpperInvariant();
        item.IdentificacaoEndereco = request.IdentificacaoEndereco.Trim(); item.IdosoId = request.IdosoId;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("residencias/{id:int}")]
    public async Task<IActionResult> DeactivateResidencia(int id, CancellationToken ct)
    {
        var item = await db.Residencias.Include(x => x.Dispositivo).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.Ativo = false; if (item.Dispositivo is not null) item.Dispositivo.Ativo = false; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("dispositivos")]
    public async Task<IActionResult> GetDispositivos(CancellationToken ct) => Ok(await db.Dispositivos.AsNoTracking().Include(x => x.Residencia)
        .Where(x => x.Ativo).Select(x => new { x.Id, x.Codigo, x.ResidenciaId, x.UltimaComunicacaoUtc }).ToListAsync(ct));

    [HttpPost("dispositivos")]
    public async Task<IActionResult> CreateDispositivo(DispositivoRequest request, CancellationToken ct)
    {
        if (!await db.Residencias.AnyAsync(x => x.Id == request.ResidenciaId && x.Ativo, ct)) return BadRequest(new { mensagem = "Residência inválida." });
        if (await db.Dispositivos.AnyAsync(x => x.Codigo == request.Codigo || x.ResidenciaId == request.ResidenciaId, ct)) return Conflict(new { mensagem = "Código já utilizado ou residência já possui dispositivo." });
        var item = new Dispositivo { Codigo = request.Codigo.Trim().ToUpperInvariant(), ResidenciaId = request.ResidenciaId, Ativo = true };
        db.Dispositivos.Add(item); await db.SaveChangesAsync(ct); return Created($"/dispositivos/{item.Id}", new { item.Id, item.Codigo });
    }

    [HttpPut("dispositivos/{id:int}")]
    public async Task<IActionResult> UpdateDispositivo(int id, DispositivoRequest request, CancellationToken ct)
    {
        var item = await db.Dispositivos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
        if (item is null) return NotFound();
        if (!await db.Residencias.AnyAsync(x => x.Id == request.ResidenciaId && x.Ativo, ct)) return BadRequest(new { mensagem = "Residência inválida." });
        if (await db.Dispositivos.AnyAsync(x => x.Id != id && (x.Codigo == request.Codigo || x.ResidenciaId == request.ResidenciaId), ct)) return Conflict(new { mensagem = "Código ou residência já utilizado." });
        item.Codigo = request.Codigo.Trim().ToUpperInvariant(); item.ResidenciaId = request.ResidenciaId;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("dispositivos/{id:int}")]
    public async Task<IActionResult> DeactivateDispositivo(int id, CancellationToken ct)
    {
        var item = await db.Dispositivos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(); item.Ativo = false; await db.SaveChangesAsync(ct); return NoContent();
    }
}

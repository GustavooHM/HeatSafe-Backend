using System.Security.Claims;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "AgenteSaude,Administrador")]
[Route("visitas")]
public sealed class VisitsController(HeatSafeDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("prioritarias")]
    public async Task<ActionResult<IReadOnlyList<VisitaPrioritariaResponse>>> Prioritarias(CancellationToken cancellationToken)
    {
        var query = db.Visitas.AsNoTracking().Include(x => x.Idoso).Include(x => x.Residencia)
            .Include(x => x.Alerta).Where(x => x.Status != StatusVisita.CANCELADA && x.Status != StatusVisita.CONCLUIDA);
        if (User.IsInRole("AgenteSaude"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var agentId)) return Forbid();
            query = query.Where(x => x.AgenteSaudeId == agentId || x.AgenteSaudeId == null);
        }

        var items = await query.OrderByDescending(x => x.PontuacaoPrioridade)
            .Select(x => new VisitaPrioritariaResponse(x.Id, x.Idoso.Nome,
                x.Residencia.IdentificacaoEndereco, x.Alerta.NivelRisco,
                x.PontuacaoPrioridade, x.MotivoPrioridade, x.CriadaEmUtc, x.Status))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(int id, AtualizarStatusVisitaRequest request, CancellationToken cancellationToken)
    {
        var visit = await db.Visitas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (visit is null) return NotFound();
        if (User.IsInRole("AgenteSaude") &&
            (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var agentId) ||
             (visit.AgenteSaudeId.HasValue && visit.AgenteSaudeId != agentId))) return Forbid();
        visit.Status = request.Status;
        visit.AtualizadaEmUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (request.Status == StatusVisita.CONCLUIDA)
        {
            var alert = await db.Alertas.FirstOrDefaultAsync(x => x.Id == visit.AlertaId, cancellationToken);
            if (alert is not null && alert.Status != StatusAlerta.CONFIRMADO)
            {
                alert.Status = StatusAlerta.RESOLVIDO;
                alert.ResolvidoEmUtc = visit.AtualizadaEmUtc;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

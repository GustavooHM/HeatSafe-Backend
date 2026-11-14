using System.Security.Claims;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Familiar")]
[Route("alertas")]
public sealed class AlertsController(HeatSafeDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("ativos")]
    public async Task<ActionResult<IReadOnlyList<AlertaResponse>>> GetActive(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var familiarId))
            return Forbid();
        var ids = db.FamiliaresIdosos.Where(x => x.FamiliarId == familiarId).Select(x => x.IdosoId);
        var items = await db.Alertas.AsNoTracking().Include(x => x.Idoso)
            .Where(x => ids.Contains(x.IdosoId) && (x.Status == StatusAlerta.ATIVO || x.Status == StatusAlerta.ESCALONADO))
            .OrderByDescending(x => x.CriadoEmUtc)
            .Select(x => new AlertaResponse(x.Id, x.Idoso.Nome, x.IndiceCalorC, x.NivelRisco,
                x.Tipo, x.Status, x.CriadoEmUtc, x.Motivo)).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, ConfirmarAlertaRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var tokenFamiliarId) ||
            tokenFamiliarId != request.FamiliarId) return Forbid();

        var alert = await db.Alertas.Include(x => x.Idoso).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (alert is null) return NotFound();
        var linked = await db.FamiliaresIdosos.AnyAsync(x => x.FamiliarId == tokenFamiliarId && x.IdosoId == alert.IdosoId, cancellationToken);
        if (!linked) return Forbid();
        if (alert.Status is StatusAlerta.CONFIRMADO or StatusAlerta.RESOLVIDO)
            return Conflict(new { mensagem = "Este alerta já foi confirmado ou resolvido." });

        alert.Status = StatusAlerta.CONFIRMADO;
        alert.ConfirmadoEmUtc = timeProvider.GetUtcNow().UtcDateTime;
        alert.FamiliarConfirmacaoId = tokenFamiliarId;
        if (!string.IsNullOrWhiteSpace(request.Observacao))
            alert.Motivo = $"{alert.Motivo} Confirmação familiar: {request.Observacao.Trim()}";
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

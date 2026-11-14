using System.Security.Claims;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Familiar,AgenteSaude,Idoso,Administrador")]
[Route("residencias")]
public sealed class StatusController(HeatSafeDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("{id:int}/status")]
    [ProducesResponseType<ResidenciaStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResidenciaStatusResponse>> Status(int id, CancellationToken cancellationToken)
    {
        var residence = await db.Residencias.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);
        if (residence is null) return NotFound();

        if (User.IsInRole("Familiar"))
        {
            if (!TryGetSubjectId(out var familiarId)) return Forbid();
            var isLinked = await db.FamiliaresIdosos.AsNoTracking()
                .AnyAsync(x => x.FamiliarId == familiarId && x.IdosoId == residence.IdosoId, cancellationToken);
            if (!isLinked) return Forbid();
        }
        else if (User.IsInRole("Idoso"))
        {
            if (!TryGetSubjectId(out var idosoId) || idosoId != residence.IdosoId) return Forbid();
        }

        var latest = await db.Leituras.AsNoTracking()
            .Where(x => x.Dispositivo.ResidenciaId == id)
            .OrderByDescending(x => x.DataHoraUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var alertActive = await db.Alertas.AsNoTracking().AnyAsync(x => x.ResidenciaId == id &&
            (x.Status == StatusAlerta.ATIVO || x.Status == StatusAlerta.ESCALONADO), cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var vigilance = residence.VigilanciaAteUtc.HasValue && residence.VigilanciaAteUtc.Value > now;

        return Ok(new ResidenciaStatusResponse(
            latest?.IndiceCalorCalculadoC ?? 0,
            latest?.NivelRisco ?? NivelRisco.NORMAL,
            residence.UltimaMovimentacaoUtc,
            residence.UltimaHidratacaoUtc,
            alertActive,
            vigilance,
            residence.VigilanciaAteUtc));
    }

    private bool TryGetSubjectId(out int id)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out id);
    }
}

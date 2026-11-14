
using System.Security.Claims;
using HeatSafe.Api.DTOs;
using HeatSafe.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Dispositivo")]
[Route("")]
public sealed class IntegrationController(DeviceIntegrationService integration) : ControllerBase
{
    [HttpPost("leituras")]
    [ProducesResponseType<LeituraResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LeituraResponse>> ReceberLeitura(
        LeituraRequest request,
        CancellationToken cancellationToken)
    {
        if (!DeviceMatches(request.DispositivoId))
            return Forbid();

        try
        {
            var result = await integration.RegistrarLeituraAsync(
                request.DispositivoId,
                request.Temperatura,
                request.Umidade,
                request.IndiceCalor,
                request.Nivel,
                request.Movimento,
                request.DataHora,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new LeituraResponse(
                    result.Id,
                    result.IndiceCalorCalculado,
                    result.IndiceCalorInformado,
                    result.Nivel,
                    result.DivergenciaNivelDispositivo));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("eventos")]
    [ProducesResponseType<EventoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EventoResponse>> ReceberEvento(
        EventoRequest request,
        CancellationToken cancellationToken)
    {
        if (!DeviceMatches(request.DispositivoId))
            return Forbid();

        try
        {
            var registered = await integration.RegistrarEventoAsync(
                request.DispositivoId,
                request.Tipo,
                request.DataHora,
                Request.Headers["Idempotency-Key"].FirstOrDefault(),
                cancellationToken);

            return Ok(new EventoResponse(
                registered,
                registered
                    ? "Evento registrado."
                    : "Evento duplicado ignorado."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    private bool DeviceMatches(string requestCode) =>
        string.Equals(
            User.FindFirstValue("device_code"),
            requestCode,
            StringComparison.OrdinalIgnoreCase);
}

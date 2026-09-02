using CashFlow.Application.CashEntries.Commands.RegisterCashEntry;
using CashFlow.Application.CashEntries.Dtos;
using CashFlow.Application.CashEntries.Queries.ListCashEntries;
using CashFlow.Application.Common.Models;
using CashFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

[ApiController]
[Route("api/lancamentos")]
public class CashEntriesController(ISender sender) : ControllerBase
{
    /// <summary>Registra um lançamento (crédito ou débito) no fluxo de caixa.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterCashEntryCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        // Não há endpoint de busca por Id individual (fora do escopo do desafio); o Location
        // aponta para a listagem, que é onde o recurso criado pode ser encontrado.
        return Created("/api/lancamentos", id);
    }

    /// <summary>Lista lançamentos, com filtros opcionais de período e tipo.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CashEntryDto>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] EntryType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListCashEntriesQuery(from, to, type, page, pageSize), cancellationToken);
        return Ok(result);
    }
}

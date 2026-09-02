using CashFlow.Application.Balances.Dtos;
using CashFlow.Application.Balances.Queries.GetBalanceRange;
using CashFlow.Application.Balances.Queries.GetDailyBalance;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

[ApiController]
[Route("api/saldo-diario")]
public class BalancesController(ISender sender) : ControllerBase
{
    /// <summary>Saldo consolidado de um dia específico. 404 se ainda não houver saldo processado para a data.</summary>
    [HttpGet("{date:datetime}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyBalanceDto>> GetByDate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDailyBalanceQuery(date), cancellationToken);

        return result is null
            ? NotFound(new { message = $"Saldo do dia {date:yyyy-MM-dd} ainda não foi consolidado ou não há lançamentos para essa data." })
            : Ok(result);
    }

    /// <summary>Saldo consolidado por período (inclusive).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyBalanceDto>>> GetByRange(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBalanceRangeQuery(from, to), cancellationToken);
        return Ok(result);
    }
}

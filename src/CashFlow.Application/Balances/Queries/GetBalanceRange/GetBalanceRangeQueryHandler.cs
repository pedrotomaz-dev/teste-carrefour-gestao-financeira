using CashFlow.Application.Balances.Dtos;
using CashFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Balances.Queries.GetBalanceRange;

public class GetBalanceRangeQueryHandler(IAppDbContext context) : IRequestHandler<GetBalanceRangeQuery, IReadOnlyList<DailyBalanceDto>>
{
    public async Task<IReadOnlyList<DailyBalanceDto>> Handle(GetBalanceRangeQuery request, CancellationToken cancellationToken)
    {
        return await context.DailyBalances
            .AsNoTracking()
            .Where(b => b.Date >= request.From && b.Date <= request.To)
            .OrderBy(b => b.Date)
            .Select(b => new DailyBalanceDto(b.Date, b.TotalCredits, b.TotalDebits, b.Balance, b.LastUpdatedAt))
            .ToListAsync(cancellationToken);
    }
}

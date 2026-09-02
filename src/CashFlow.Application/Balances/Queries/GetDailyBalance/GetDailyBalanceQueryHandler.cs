using CashFlow.Application.Balances.Dtos;
using CashFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Balances.Queries.GetDailyBalance;

public class GetDailyBalanceQueryHandler(IAppDbContext context) : IRequestHandler<GetDailyBalanceQuery, DailyBalanceDto?>
{
    public async Task<DailyBalanceDto?> Handle(GetDailyBalanceQuery request, CancellationToken cancellationToken)
    {
        return await context.DailyBalances
            .AsNoTracking()
            .Where(b => b.Date == request.Date)
            .Select(b => new DailyBalanceDto(b.Date, b.TotalCredits, b.TotalDebits, b.Balance, b.LastUpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

using CashFlow.Application.Balances.Dtos;
using MediatR;

namespace CashFlow.Application.Balances.Queries.GetBalanceRange;

public record GetBalanceRangeQuery(DateOnly From, DateOnly To) : IRequest<IReadOnlyList<DailyBalanceDto>>;

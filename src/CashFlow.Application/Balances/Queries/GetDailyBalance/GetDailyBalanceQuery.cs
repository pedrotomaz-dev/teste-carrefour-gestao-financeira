using CashFlow.Application.Balances.Dtos;
using MediatR;

namespace CashFlow.Application.Balances.Queries.GetDailyBalance;

public record GetDailyBalanceQuery(DateOnly Date) : IRequest<DailyBalanceDto?>;

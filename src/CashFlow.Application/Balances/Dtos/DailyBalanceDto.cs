namespace CashFlow.Application.Balances.Dtos;

public record DailyBalanceDto(DateOnly Date, decimal TotalCredits, decimal TotalDebits, decimal Balance, DateTime LastUpdatedAt);

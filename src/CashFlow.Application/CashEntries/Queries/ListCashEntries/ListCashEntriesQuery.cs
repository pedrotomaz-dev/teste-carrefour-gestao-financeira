using CashFlow.Application.CashEntries.Dtos;
using CashFlow.Application.Common.Models;
using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.CashEntries.Queries.ListCashEntries;

public record ListCashEntriesQuery(
    DateOnly? From,
    DateOnly? To,
    EntryType? Type,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CashEntryDto>>;

using CashFlow.Application.CashEntries.Dtos;
using CashFlow.Application.Common.Models;
using CashFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.CashEntries.Queries.ListCashEntries;

public class ListCashEntriesQueryHandler(IAppDbContext context) : IRequestHandler<ListCashEntriesQuery, PagedResult<CashEntryDto>>
{
    public async Task<PagedResult<CashEntryDto>> Handle(ListCashEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = context.CashEntries.AsNoTracking().AsQueryable();

        if (request.From.HasValue)
        {
            query = query.Where(e => e.OccurredOn >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(e => e.OccurredOn <= request.To.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(e => e.Type == request.Type.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(e => e.OccurredOn)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new CashEntryDto(e.Id, e.Description, e.Amount, e.Type, e.OccurredOn, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CashEntryDto>(items, page, pageSize, totalCount);
    }
}

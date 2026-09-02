using CashFlow.Application.CashEntries.Queries.ListCashEntries;
using CashFlow.Application.Tests.TestSupport;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.Application.Tests.CashEntries;

public class ListCashEntriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_FiltersByTypeAndPeriod()
    {
        using var context = TestDbContext.CreateInMemory();
        context.CashEntries.AddRange(
            new CashEntry("Venda 1", 10, EntryType.Credit, new DateOnly(2026, 9, 1)),
            new CashEntry("Compra 1", 20, EntryType.Debit, new DateOnly(2026, 9, 1)),
            new CashEntry("Venda 2", 30, EntryType.Credit, new DateOnly(2026, 8, 1)));
        await context.SaveChangesAsync();

        var handler = new ListCashEntriesQueryHandler(context);
        var result = await handler.Handle(
            new ListCashEntriesQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), EntryType.Credit), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Description.Should().Be("Venda 1");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsRequestedPage()
    {
        using var context = TestDbContext.CreateInMemory();
        for (var i = 1; i <= 5; i++)
        {
            context.CashEntries.Add(new CashEntry($"Lançamento {i}", i, EntryType.Credit, new DateOnly(2026, 9, i)));
        }
        await context.SaveChangesAsync();

        var handler = new ListCashEntriesQueryHandler(context);
        var result = await handler.Handle(new ListCashEntriesQuery(null, null, null, Page: 2, PageSize: 2), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
    }
}

using System.Net;
using System.Net.Http.Json;
using CashFlow.Api.Tests.TestSupport;
using CashFlow.Application.Balances.Dtos;
using CashFlow.Application.Common.Models;
using CashFlow.Application.CashEntries.Dtos;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.Api.Tests;

public class CashEntriesEndpointsTests : IClassFixture<CashFlowApiFactory>
{
    private readonly HttpClient _client;

    public CashEntriesEndpointsTests(CashFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenList_ReturnsTheCreatedEntry()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.PostAsJsonAsync("/api/lancamentos", new
        {
            description = "Venda no cartão",
            amount = 150.75m,
            type = EntryType.Credit,
            occurredOn = today
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();

        var listResponse = await _client.GetFromJsonAsync<PagedResult<CashEntryDto>>($"/api/lancamentos?from={today}&to={today}");

        listResponse!.Items.Should().ContainSingle(e => e.Id == id && e.Amount == 150.75m);
    }

    [Fact]
    public async Task Register_WithInvalidAmount_ReturnsBadRequestWithErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/lancamentos", new
        {
            description = "Inválido",
            amount = -5,
            type = EntryType.Debit
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Amount");
    }

    [Fact]
    public async Task Register_CreditAndDebit_EventuallyConsolidatesDailyBalance()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var credit = await _client.PostAsJsonAsync("/api/lancamentos", new { description = "Venda", amount = 200m, type = EntryType.Credit, occurredOn = date });
        var debit = await _client.PostAsJsonAsync("/api/lancamentos", new { description = "Despesa", amount = 80m, type = EntryType.Debit, occurredOn = date });
        credit.EnsureSuccessStatusCode();
        debit.EnsureSuccessStatusCode();

        var balance = await PollUntilBalanceExistsAsync(date);

        balance.Should().NotBeNull();
        balance!.TotalCredits.Should().Be(200m);
        balance.TotalDebits.Should().Be(80m);
        balance.Balance.Should().Be(120m);
    }

    [Fact]
    public async Task GetDailyBalance_ForDateWithoutEntries_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/saldo-diario/2019-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<DailyBalanceDto?> PollUntilBalanceExistsAsync(string date)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetAsync($"/api/saldo-diario/{date}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<DailyBalanceDto>();
            }

            await Task.Delay(250);
        }

        return null;
    }
}

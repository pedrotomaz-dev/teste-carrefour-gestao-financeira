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

        // As duas mensagens (crédito e débito) são consolidadas de forma assíncrona e
        // independente uma da outra — não basta esperar a linha de saldo existir (ela pode
        // aparecer já com o crédito aplicado e o débito ainda em trânsito); é preciso esperar o
        // valor final esperado, senão o teste fica intermitente.
        var balance = await PollUntilAsync(date, b => b.TotalCredits == 200m && b.TotalDebits == 80m);

        balance.Should().NotBeNull("o worker deveria ter consolidado os dois lançamentos dentro do timeout");
        balance!.Balance.Should().Be(120m);
    }

    [Fact]
    public async Task GetDailyBalance_ForDateWithoutEntries_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/saldo-diario/2019-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<DailyBalanceDto?> PollUntilAsync(string date, Func<DailyBalanceDto, bool> isExpected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        DailyBalanceDto? lastSeen = null;

        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetAsync($"/api/saldo-diario/{date}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                lastSeen = await response.Content.ReadFromJsonAsync<DailyBalanceDto>();
                if (lastSeen is not null && isExpected(lastSeen))
                {
                    return lastSeen;
                }
            }

            await Task.Delay(250);
        }

        // Retorna o último estado observado (mesmo que parcial) para a asserção do teste falhar
        // com um diagnóstico útil, em vez de simplesmente "null".
        return lastSeen;
    }
}

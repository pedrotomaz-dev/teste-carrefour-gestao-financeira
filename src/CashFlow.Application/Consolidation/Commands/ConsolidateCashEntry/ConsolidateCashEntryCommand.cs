using CashFlow.Domain.Events;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateCashEntry;

/// <summary>
/// Comando enviado pelo adaptador de mensageria (consumer RabbitMQ/InMemory) do
/// CashFlow.ConsolidationWorker sempre que um <see cref="CashEntryRegisteredEvent"/> chega.
/// Mantê-lo como um Command traduzido pelo consumer (em vez de manipular o DbContext
/// diretamente na infraestrutura) permite testar a regra de consolidação isoladamente.
/// </summary>
public record ConsolidateCashEntryCommand(CashEntryRegisteredEvent Event) : IRequest;

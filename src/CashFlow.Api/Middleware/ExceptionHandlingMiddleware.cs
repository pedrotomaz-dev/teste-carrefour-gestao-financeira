using System.Net;
using CashFlow.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Middleware;

/// <summary>
/// Ponto único de tratamento de erros da Api: traduz exceções de domínio/validação em respostas
/// HTTP previsíveis (ProblemDetails) e garante que exceções inesperadas nunca vazem stack trace
/// para o cliente, apenas um 500 genérico (o detalhe vai para o log).
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Erro de validação", ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao processar {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado. Tente novamente mais tarde.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, IDictionary<string, string[]>? errors = null)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        // Serializa pelo tipo em tempo de execução: se usássemos uma variável estática do tipo
        // base `ProblemDetails`, o System.Text.Json ignoraria a propriedade `Errors`, que só
        // existe em `ValidationProblemDetails`.
        if (errors is null)
        {
            var problem = new ProblemDetails { Status = (int)statusCode, Title = title };
            await context.Response.WriteAsJsonAsync(problem);
        }
        else
        {
            var problem = new ValidationProblemDetails(errors) { Status = (int)statusCode, Title = title };
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

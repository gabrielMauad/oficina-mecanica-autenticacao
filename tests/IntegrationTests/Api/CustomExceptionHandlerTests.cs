using Api.Middlewares;
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Api;

public class CustomExceptionHandlerTests
{
    private sealed class ProblemDetailsServiceFalso : IProblemDetailsService
    {
        public ProblemDetailsContext? Contexto { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Contexto = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Contexto = context;
            return ValueTask.FromResult(true);
        }
    }

    [Theory(DisplayName = "TryHandleAsync — mapeia ArgumentException para 400 e demais exceções para 500")]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError)]
    public async Task TryHandleAsync_DeveMapearStatusEEscreverProblemDetails(Type tipoExcecao, int statusEsperado)
    {
        var servico = new ProblemDetailsServiceFalso();
        var handler = new CustomExceptionHandler(servico);
        var httpContext = new DefaultHttpContext();
        var excecao = (Exception)Activator.CreateInstance(tipoExcecao, "falha simulada")!;

        var tratado = await handler.TryHandleAsync(httpContext, excecao, TestContext.Current.CancellationToken);

        Assert.True(tratado);
        Assert.Equal(statusEsperado, httpContext.Response.StatusCode);
        Assert.NotNull(servico.Contexto);
        Assert.Same(excecao, servico.Contexto.Exception);
        var problema = servico.Contexto.ProblemDetails;
        Assert.Equal("An error occurred", problema.Title);
        Assert.Equal(tipoExcecao.Name, problema.Type);
        Assert.Equal("falha simulada", problema.Detail);
    }
}

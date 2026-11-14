using System.Diagnostics;
using System.Text.Json;
using Api.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.Api;

public class TraceJsonConsoleFormatterTests
{
    private static JsonElement Escrever(
        LogLevel nivel,
        string mensagem,
        IReadOnlyList<KeyValuePair<string, object>>? estado = null,
        Exception? excecao = null,
        IExternalScopeProvider? escopos = null)
    {
        var entrada = new LogEntry<IReadOnlyList<KeyValuePair<string, object>>>(
            nivel,
            "Categoria.Teste",
            new EventId(1),
            estado ?? [],
            excecao,
            (_, _) => mensagem);

        using var saida = new StringWriter();
        new TraceJsonConsoleFormatter().Write(entrada, escopos, saida);

        var linha = saida.ToString();
        Assert.EndsWith(Environment.NewLine, linha);
        return JsonDocument.Parse(linha).RootElement.Clone();
    }

    [Theory(DisplayName = "Write — emite o nível de log por extenso")]
    [InlineData(LogLevel.Trace, "Trace")]
    [InlineData(LogLevel.Debug, "Debug")]
    [InlineData(LogLevel.Information, "Information")]
    [InlineData(LogLevel.Warning, "Warning")]
    [InlineData(LogLevel.Error, "Error")]
    [InlineData(LogLevel.Critical, "Critical")]
    [InlineData(LogLevel.None, "None")]
    public void Write_DeveEmitirNivel(LogLevel nivel, string esperado)
    {
        var json = Escrever(nivel, "mensagem");

        Assert.Equal(esperado, json.GetProperty("level").GetString());
        Assert.Equal("mensagem", json.GetProperty("message").GetString());
        Assert.Equal("Categoria.Teste", json.GetProperty("category").GetString());
    }

    [Fact(DisplayName = "Write — sem Activity, trace_id e span_id são nulos")]
    public void Write_SemActivity_DeveEmitirTraceNulo()
    {
        Activity.Current = null;

        var json = Escrever(LogLevel.Information, "mensagem");

        Assert.Equal(JsonValueKind.Null, json.GetProperty("trace_id").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("span_id").ValueKind);
    }

    [Fact(DisplayName = "Write — com Activity, emite trace_id e span_id")]
    public void Write_ComActivity_DeveEmitirTraceESpan()
    {
        using var activity = new Activity("teste");
        activity.Start();

        var json = Escrever(LogLevel.Information, "mensagem");

        Assert.Equal(activity.TraceId.ToHexString(), json.GetProperty("trace_id").GetString());
        Assert.Equal(activity.SpanId.ToHexString(), json.GetProperty("span_id").GetString());
    }

    [Fact(DisplayName = "Write — propriedades do estado são emitidas com o tipo JSON correspondente")]
    public void Write_DeveEmitirPropriedadesDoEstadoComTipos()
    {
        var momento = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var estado = new List<KeyValuePair<string, object>>
        {
            new("{OriginalFormat}", "ignorado"),
            new("texto", "abc"),
            new("flag", true),
            new("inteiro", 7),
            new("decimal", 1.5m),
            new("data", momento),
            new("dataOffset", new DateTimeOffset(momento)),
            new("guid", Guid.Empty),
            new("nulo", null!),
        };

        var json = Escrever(LogLevel.Information, "mensagem", estado);

        Assert.False(json.TryGetProperty("{OriginalFormat}", out _));
        Assert.Equal("abc", json.GetProperty("texto").GetString());
        Assert.True(json.GetProperty("flag").GetBoolean());
        Assert.Equal(7, json.GetProperty("inteiro").GetInt64());
        Assert.Equal(1.5, json.GetProperty("decimal").GetDouble());
        Assert.Equal(momento.ToString("O"), json.GetProperty("data").GetString());
        Assert.Equal(new DateTimeOffset(momento).ToString("O"), json.GetProperty("dataOffset").GetString());
        Assert.Equal(Guid.Empty.ToString(), json.GetProperty("guid").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nulo").ValueKind);
    }

    [Fact(DisplayName = "Write — propriedades dos escopos são emitidas")]
    public void Write_DeveEmitirPropriedadesDosEscopos()
    {
        var escopos = new LoggerExternalScopeProvider();
        using var escopoComPropriedades = escopos.Push(new List<KeyValuePair<string, object>> { new("correlacao", "abc-123") });
        using var escopoSimples = escopos.Push("escopo sem propriedades");

        var json = Escrever(LogLevel.Information, "mensagem", escopos: escopos);

        Assert.Equal("abc-123", json.GetProperty("correlacao").GetString());
    }

    [Fact(DisplayName = "Write — exceção é emitida com tipo, mensagem e stack trace")]
    public void Write_DeveEmitirExcecao()
    {
        Exception excecao;
        try { throw new InvalidOperationException("quebrou"); }
        catch (Exception ex) { excecao = ex; }

        var json = Escrever(LogLevel.Error, "mensagem", excecao: excecao);

        var bloco = json.GetProperty("exception");
        Assert.Equal(typeof(InvalidOperationException).FullName, bloco.GetProperty("type").GetString());
        Assert.Equal("quebrou", bloco.GetProperty("message").GetString());
        Assert.False(string.IsNullOrEmpty(bloco.GetProperty("stack_trace").GetString()));
    }

    [Fact(DisplayName = "Write — mensagem vazia sem exceção não escreve nada")]
    public void Write_MensagemVaziaSemExcecao_NaoDeveEscrever()
    {
        var entrada = new LogEntry<string>(LogLevel.Information, "Cat", new EventId(1), "x", null, (_, _) => "");
        using var saida = new StringWriter();

        new TraceJsonConsoleFormatter().Write(entrada, null, saida);

        Assert.Equal(string.Empty, saida.ToString());
    }
}

using SharedKernel.Domain;

namespace SharedKernel.Application.Tests.Domain;

public class EntityEValueObjectTests
{
    private sealed record EventoDominio : IDomainEvent
    {
        public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
    }

    private sealed class EntidadeA(Guid id) : Entity<Guid>(id);

    private sealed class EntidadeB(Guid id) : Entity<Guid>(id);

    private sealed class Agregado(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Disparar() => AddDomainEvent(new EventoDominio());
    }

    private sealed class Dinheiro(decimal valor, string moeda) : ValueObject
    {
        protected override IEnumerable<object?> GetComponents()
        {
            yield return valor;
            yield return moeda;
        }
    }

    [Fact(DisplayName = "Entity — igualdade é por tipo e Id")]
    public void Entity_Igualdade_DeveConsiderarTipoEId()
    {
        var id = Guid.NewGuid();
        var a1 = new EntidadeA(id);
        var a2 = new EntidadeA(id);

        Assert.True(a1 == a2);
        Assert.True(a1.Equals(a1));
        Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        Assert.True(a1 != new EntidadeA(Guid.NewGuid()));
        Assert.False(a1.Equals(new EntidadeB(id)));
        Assert.False(a1.Equals("outro objeto"));
        Assert.True((EntidadeA?)null == (EntidadeA?)null);
        Assert.False(a1 == null);
    }

    [Fact(DisplayName = "AggregateRoot — acumula eventos de domínio e permite limpar")]
    public void AggregateRoot_DeveAcumularELimparEventos()
    {
        var agregado = new Agregado(Guid.NewGuid());

        agregado.Disparar();
        agregado.Disparar();
        Assert.Equal(2, agregado.DomainEvents.Count);

        agregado.ClearDomainEvents();
        Assert.Empty(agregado.DomainEvents);
    }

    [Fact(DisplayName = "ValueObject — igualdade é pelos componentes")]
    public void ValueObject_Igualdade_DeveConsiderarComponentes()
    {
        var a = new Dinheiro(10m, "BRL");
        var b = new Dinheiro(10m, "BRL");

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a != new Dinheiro(11m, "BRL"));
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("10 BRL"));
        Assert.True((Dinheiro?)null == (Dinheiro?)null);
        Assert.False(a == null);
    }
}

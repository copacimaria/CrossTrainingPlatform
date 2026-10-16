using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Common.Behaviors;
using FluentValidation;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

// Mesaje de test: pipeline-ul se comporta diferit dupa cum cererea e comanda sau interogare.
public sealed record SampleCommand(string Value) : ICommand<string>;

public sealed record SampleQuery(string Value) : IQuery<string>;

public sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
{
    public SampleCommandValidator() => RuleFor(c => c.Value).NotEmpty();
}

public class ValidationBehaviorTests
{
    private static Task<string> Next() => Task.FromResult("ok");

    [Fact]
    public async Task CerereValida_AjungeLaHandler()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);

        var result = await behavior.Handle(new SampleCommand("ceva"), Next, default);

        result.ShouldBe("ok");
    }

    [Fact]
    public async Task CerereInvalida_NuAjungeLaHandler()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);
        var handlerWasCalled = false;

        Task<string> next()
        {
            handlerWasCalled = true;
            return Task.FromResult("ok");
        }

        await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(new SampleCommand(""), next, default));

        handlerWasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task FaraValidatoare_TreceMaiDeparte()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([]);

        (await behavior.Handle(new SampleCommand(""), Next, default)).ShouldBe("ok");
    }
}

public class UnitOfWorkBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUnitOfWorkTransaction _transaction = Substitute.For<IUnitOfWorkTransaction>();

    public UnitOfWorkBehaviorTests()
        => _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

    [Fact]
    public async Task Comanda_RuleazaInTranzactieSiSeConfirma()
    {
        var behavior = new UnitOfWorkBehavior<SampleCommand, string>(_unitOfWork);

        await behavior.Handle(new SampleCommand("x"), () => Task.FromResult("ok"), default);

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Interogare_NuDeschideTranzactie()
    {
        // O citire nu are ce comite; tranzactia ar fi cost pur.
        var behavior = new UnitOfWorkBehavior<SampleQuery, string>(_unitOfWork);

        await behavior.Handle(new SampleQuery("x"), () => Task.FromResult("ok"), default);

        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComandaCareArunca_NuSeConfirma()
    {
        // Fara Commit, Dispose anuleaza tranzactia, deci modificarile partiale se pierd.
        var behavior = new UnitOfWorkBehavior<SampleCommand, string>(_unitOfWork);

        await Should.ThrowAsync<InvalidOperationException>(
            () => behavior.Handle(
                new SampleCommand("x"),
                () => throw new InvalidOperationException("esec in handler"),
                default));

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
    }
}

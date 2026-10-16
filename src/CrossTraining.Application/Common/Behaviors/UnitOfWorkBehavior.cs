using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using MediatR;

namespace CrossTraining.Application.Common.Behaviors;

/// <summary>
/// Deschide o tranzactie in jurul fiecarei comenzi si o confirma doar daca handlerul
/// s-a terminat cu bine.
///
/// Handlerele apeleaza in continuare <c>SaveChangesAsync</c> acolo unde au nevoie de
/// valori generate de baza de date (un id de identity). Tranzactia le leaga: doua
/// salvari in acelasi handler nu mai pot ramane pe jumatate aplicate.
///
/// Interogarile nu trec pe aici - nu au ce comite.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
            return await next();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var response = await next();

        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    private static bool IsCommand()
        => typeof(TRequest).GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}

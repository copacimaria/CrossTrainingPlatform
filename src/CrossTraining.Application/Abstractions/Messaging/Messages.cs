using MediatR;

namespace CrossTraining.Application.Abstractions.Messaging;

/// <summary>
/// O operatie care schimba starea sistemului.
///
/// Distinctia fata de <see cref="IQuery{TResponse}"/> nu este decorativa: pipeline-ul o
/// foloseste ca sa decida ce se aplica. Doar comenzile primesc tranzactie.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>O operatie care doar citeste. Nu deschide tranzactie.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;

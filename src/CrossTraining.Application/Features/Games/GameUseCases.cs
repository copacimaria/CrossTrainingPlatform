using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using FluentValidation;

namespace CrossTraining.Application.Features.Games;

// ------------------- CITIRI -------------------

/// <summary>Jocurile vizibile unui utilizator: toate cele active, plus cele din domeniile lui.</summary>
public sealed record GetGamesForUserQuery(int UserId) : IQuery<GamesForUserDto>;

public sealed class GetGamesForUserValidator : AbstractValidator<GetGamesForUserQuery>
{
    public GetGamesForUserValidator() => RuleFor(q => q.UserId).GreaterThan(0);
}

public sealed class GetGamesForUserHandler : IQueryHandler<GetGamesForUserQuery, GamesForUserDto>
{
    private readonly IGameRepository _games;
    private readonly IAppUserRepository _users;

    public GetGamesForUserHandler(IGameRepository games, IAppUserRepository users)
    {
        _games = games;
        _users = users;
    }

    public async Task<GamesForUserDto> Handle(GetGamesForUserQuery query, CancellationToken cancellationToken)
    {
        // Doua interogari, nu trei: domeniul vine cu jocul, prin Include.
        var domainIds = await _users.GetDomainIdsAsync(query.UserId);
        var games = await _games.GetActiveWithDomainAsync();

        var all = games.Select(GameMapping.ToDto).ToList();

        var forUser = all
            .Where(g => g.DomainId != null && domainIds.Contains(g.DomainId.Value))
            .ToList();

        return new GamesForUserDto(all, forUser);
    }
}

public sealed record GetGameQuery(int GameId) : IQuery<GameDto?>;

public sealed class GetGameHandler : IQueryHandler<GetGameQuery, GameDto?>
{
    private readonly IGameRepository _games;

    public GetGameHandler(IGameRepository games) => _games = games;

    public async Task<GameDto?> Handle(GetGameQuery query, CancellationToken cancellationToken)
    {
        var game = await _games.GetForAdminAsync(query.GameId);
        return game == null ? null : GameMapping.ToDto(game);
    }
}

// ------------------- COMENZI -------------------

public sealed record CreateGameCommand(
    string GameName,
    string? Description,
    string? DownloadLink,
    int? DomainId) : ICommand<int>;

public sealed class CreateGameValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameValidator()
    {
        RuleFor(c => c.GameName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Description).MaximumLength(500);
        RuleFor(c => c.DownloadLink).MaximumLength(500);
    }
}

public sealed class CreateGameHandler : ICommandHandler<CreateGameCommand, int>
{
    private readonly IGameRepository _games;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CreateGameHandler(IGameRepository games, IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _games = games;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<int> Handle(CreateGameCommand command, CancellationToken cancellationToken)
    {
        var game = new Game
        {
            GameName = command.GameName,
            Description = command.Description,
            DownloadLink = command.DownloadLink,
            DomainId = command.DomainId,
            IsActive = true,
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        };

        _games.Add(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return game.GameId;
    }
}

public sealed record UpdateGameCommand(
    int GameId,
    string GameName,
    string? Description,
    string? DownloadLink,
    int? DomainId) : ICommand<bool>;

public sealed class UpdateGameValidator : AbstractValidator<UpdateGameCommand>
{
    public UpdateGameValidator()
    {
        RuleFor(c => c.GameId).GreaterThan(0);
        RuleFor(c => c.GameName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Description).MaximumLength(500);
        RuleFor(c => c.DownloadLink).MaximumLength(500);
    }
}

public sealed class UpdateGameHandler : ICommandHandler<UpdateGameCommand, bool>
{
    private readonly IGameRepository _games;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGameHandler(IGameRepository games, IUnitOfWork unitOfWork)
    {
        _games = games;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateGameCommand command, CancellationToken cancellationToken)
    {
        var game = await _games.GetForAdminAsync(command.GameId);
        if (game == null)
            return false;

        game.GameName = command.GameName;
        game.Description = command.Description;
        game.DownloadLink = command.DownloadLink;
        game.DomainId = command.DomainId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Dezactiveaza jocul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateGameCommand(int GameId) : ICommand<bool>;

public sealed class DeactivateGameValidator : AbstractValidator<DeactivateGameCommand>
{
    public DeactivateGameValidator() => RuleFor(c => c.GameId).GreaterThan(0);
}

public sealed class DeactivateGameHandler : ICommandHandler<DeactivateGameCommand, bool>
{
    private readonly IGameRepository _games;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateGameHandler(IGameRepository games, IUnitOfWork unitOfWork)
    {
        _games = games;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateGameCommand command, CancellationToken cancellationToken)
    {
        var game = await _games.GetForAdminAsync(command.GameId);
        if (game == null)
            return false;

        game.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class GameMapping
{
    public static GameDto ToDto(Game game)
        => new(
            game.GameId,
            game.GameName,
            game.Description,
            game.DownloadLink,
            game.IsActive,
            game.DomainId,
            game.Domain?.DomainName);
}

using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Features.Games;

// ------------------- CITIRI -------------------

/// <summary>Jocurile vizibile unui utilizator: toate cele active, plus cele din domeniile lui.</summary>
public sealed record GetGamesForUserQuery(int UserId) : IQuery<GamesForUserDto>;

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
    int? DomainId) : ICommand<OperationResult>;

public sealed class CreateGameHandler : ICommandHandler<CreateGameCommand, OperationResult>
{
    private readonly IGameRepository _games;
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public CreateGameHandler(
        IGameRepository games,
        IDomainRepository domains,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _games = games;
        _domains = domains;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<OperationResult> Handle(CreateGameCommand command, CancellationToken cancellationToken)
    {
        var missingDomain = await GameRules.CheckDomainExistsAsync(_domains, command.DomainId);
        if (missingDomain != null)
            return missingDomain;

        var created = Game.Create(
            command.GameName,
            command.Description,
            command.DownloadLink,
            command.DomainId,
            _clock.GetUtcNow().UtcDateTime);

        if (!created.Succeeded)
            return OperationResult.From(created);

        _games.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record UpdateGameCommand(
    int GameId,
    string GameName,
    string? Description,
    string? DownloadLink,
    int? DomainId) : ICommand<OperationResult>;

public sealed class UpdateGameHandler : ICommandHandler<UpdateGameCommand, OperationResult>
{
    private readonly IGameRepository _games;
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGameHandler(IGameRepository games, IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _games = games;
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(UpdateGameCommand command, CancellationToken cancellationToken)
    {
        var game = await _games.GetForAdminAsync(command.GameId);
        if (game == null)
            return GameRules.NotFound();

        var missingDomain = await GameRules.CheckDomainExistsAsync(_domains, command.DomainId);
        if (missingDomain != null)
            return missingDomain;

        var updated = game.Update(command.GameName, command.Description, command.DownloadLink, command.DomainId);
        if (!updated.Succeeded)
            return OperationResult.From(updated);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza jocul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateGameCommand(int GameId) : ICommand<OperationResult>;

public sealed class DeactivateGameHandler : ICommandHandler<DeactivateGameCommand, OperationResult>
{
    private readonly IGameRepository _games;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateGameHandler(IGameRepository games, IUnitOfWork unitOfWork)
    {
        _games = games;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(DeactivateGameCommand command, CancellationToken cancellationToken)
    {
        var game = await _games.GetForAdminAsync(command.GameId);
        if (game == null)
            return GameRules.NotFound();

        game.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

internal static class GameRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Jocul nu exista.");

    /// <summary>
    /// Refuzul, daca domeniul ales nu exista; altfel null. Verificat aici, nu lasat pe seama
    /// cheii straine, ca un id gresit sa ajunga la utilizator ca mesaj, nu ca eroare 500.
    /// </summary>
    public static async Task<OperationResult?> CheckDomainExistsAsync(IDomainRepository domains, int? domainId)
    {
        if (domainId == null)
            return null;

        return await domains.GetForAdminAsync(domainId.Value) == null
            ? OperationResult.Fail("Domeniul ales nu exista.")
            : null;
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

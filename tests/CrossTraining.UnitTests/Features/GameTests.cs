using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Features.Games;
using CrossTraining.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

public class GetGamesForUserTests
{
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();

    private GetGamesForUserHandler CreateSut() => new(_games, _users);

    private static Game GameIn(int gameId, int? domainId, string? domainName)
        => new()
        {
            GameId = gameId,
            GameName = $"joc-{gameId}",
            DomainId = domainId,
            Domain = domainName == null || domainId == null
                ? null
                : new TrainingDomain { DomainId = domainId.Value, DomainName = domainName }
        };

    [Fact]
    public async Task IntoarceDoarJocurileDinDomeniileUtilizatorului()
    {
        _users.GetDomainIdsAsync(1).Returns([10]);
        _games.GetActiveWithDomainAsync().Returns(
        [
            GameIn(1, 10, "Matematica"),
            GameIn(2, 20, "Chimie"),
            GameIn(3, null, null)
        ]);

        var result = await CreateSut().Handle(new GetGamesForUserQuery(1), default);

        result.GamesForSelectedDomains.Select(g => g.GameId).ShouldBe([1]);
        result.AllGames.Count.ShouldBe(3);
    }

    [Fact]
    public async Task FoloseresteOSinguraInterogareCuDomeniiIncluse()
    {
        // Regresie: inainte se faceau trei interogari (utilizator, domenii, jocuri) si
        // un join in memorie. Acum domeniul vine odata cu jocul.
        _users.GetDomainIdsAsync(1).Returns([10]);
        _games.GetActiveWithDomainAsync().Returns([GameIn(1, 10, "Matematica")]);

        var result = await CreateSut().Handle(new GetGamesForUserQuery(1), default);

        result.GamesForSelectedDomains[0].DomainName.ShouldBe("Matematica");
        await _games.Received(1).GetActiveWithDomainAsync();
    }

    [Fact]
    public async Task JocFaraDomeniu_AreNumeleDomeniuluiNull()
    {
        _users.GetDomainIdsAsync(1).Returns([]);
        _games.GetActiveWithDomainAsync().Returns([GameIn(1, null, null)]);

        var result = await CreateSut().Handle(new GetGamesForUserQuery(1), default);

        result.AllGames[0].DomainName.ShouldBeNull();
    }

    [Fact]
    public async Task UtilizatorFaraDomenii_NuPrimesteNiciunJoc()
    {
        _users.GetDomainIdsAsync(1).Returns([]);
        _games.GetActiveWithDomainAsync().Returns([GameIn(1, 10, "Matematica")]);

        var result = await CreateSut().Handle(new GetGamesForUserQuery(1), default);

        result.GamesForSelectedDomains.ShouldBeEmpty();
    }
}

public class GameCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);

    [Fact]
    public async Task Create_FoloseresteCeasulInjectatSiSalveazaOData()
    {
        var handler = new CreateGameHandler(_games, _unitOfWork, _clock);

        await handler.Handle(new CreateGameCommand("Joc nou", "descriere", null, 10), default);

        var saved = (Game)_games.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == nameof(IGameRepository.Add))
            .GetArguments()[0]!;

        saved.CreatedAt.ShouldBe(Now.UtcDateTime);
        saved.GameName.ShouldBe("Joc nou");
        saved.IsActive.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ModificaInregistrareaDinComanda()
    {
        var existing = new Game { GameId = 7, GameName = "vechi", DomainId = 10 };
        _games.GetForAdminAsync(7).Returns(existing);
        var handler = new UpdateGameHandler(_games, _unitOfWork);

        await handler.Handle(new UpdateGameCommand(7, "Redenumit", "d", null, null), default);

        existing.GameName.ShouldBe("Redenumit");
        existing.DomainId.ShouldBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_CandJoculNuExista_NuSalveazaNimic()
    {
        _games.GetForAdminAsync(99).Returns((Game?)null);
        var handler = new UpdateGameHandler(_games, _unitOfWork);

        var changed = await handler.Handle(new UpdateGameCommand(99, "x", "d", null, null), default);

        changed.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_NuStergeInregistrarea_DoarODezactiveaza()
    {
        var existing = new Game { GameId = 7, GameName = "joc", IsActive = true };
        _games.GetForAdminAsync(7).Returns(existing);
        var handler = new DeactivateGameHandler(_games, _unitOfWork);

        await handler.Handle(new DeactivateGameCommand(7), default);

        existing.IsActive.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_RespingeNumeGolSiDescriereaPreaLunga()
    {
        var validator = new CreateGameValidator();

        validator.Validate(new CreateGameCommand("", null, null, null)).IsValid.ShouldBeFalse();
        validator.Validate(new CreateGameCommand("ok", new string('x', 501), null, null))
                 .IsValid.ShouldBeFalse();
        validator.Validate(new CreateGameCommand("ok", "descriere", null, 1)).IsValid.ShouldBeTrue();
    }
}

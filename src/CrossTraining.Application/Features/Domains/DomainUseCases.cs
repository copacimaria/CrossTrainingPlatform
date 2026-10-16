using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Features.Domains;

// ------------------- CITIRI -------------------

/// <summary>Toate domeniile, inclusiv cele dezactivate (ecran de administrare).</summary>
public sealed record GetAllDomainsForAdminQuery : IQuery<List<DomainDto>>;

public sealed class GetAllDomainsForAdminHandler : IQueryHandler<GetAllDomainsForAdminQuery, List<DomainDto>>
{
    private readonly IDomainRepository _domains;

    public GetAllDomainsForAdminHandler(IDomainRepository domains) => _domains = domains;

    public async Task<List<DomainDto>> Handle(
        GetAllDomainsForAdminQuery query,
        CancellationToken cancellationToken)
        => (await _domains.GetAllForAdminAsync()).Select(DomainMapping.ToDto).ToList();
}

/// <summary>Domeniile active, pentru listele din care alege utilizatorul.</summary>
public sealed record GetActiveDomainsQuery : IQuery<List<DomainDto>>;

public sealed class GetActiveDomainsHandler : IQueryHandler<GetActiveDomainsQuery, List<DomainDto>>
{
    private readonly IDomainRepository _domains;

    public GetActiveDomainsHandler(IDomainRepository domains) => _domains = domains;

    public async Task<List<DomainDto>> Handle(GetActiveDomainsQuery query, CancellationToken cancellationToken)
        => (await _domains.GetActiveAsync()).Select(DomainMapping.ToDto).ToList();
}

public sealed record GetDomainQuery(int DomainId) : IQuery<DomainDto?>;

public sealed class GetDomainHandler : IQueryHandler<GetDomainQuery, DomainDto?>
{
    private readonly IDomainRepository _domains;

    public GetDomainHandler(IDomainRepository domains) => _domains = domains;

    public async Task<DomainDto?> Handle(GetDomainQuery query, CancellationToken cancellationToken)
    {
        var domain = await _domains.GetForAdminAsync(query.DomainId);
        return domain == null ? null : DomainMapping.ToDto(domain);
    }
}

// ------------------- COMENZI -------------------

public sealed record CreateDomainCommand(string DomainName, bool IsActive) : ICommand<OperationResult>;

public sealed class CreateDomainHandler : ICommandHandler<CreateDomainCommand, OperationResult>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(CreateDomainCommand command, CancellationToken cancellationToken)
    {
        var created = TrainingDomain.Create(command.DomainName, command.IsActive);
        if (!created.Succeeded)
            return OperationResult.From(created);

        _domains.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record UpdateDomainCommand(int DomainId, string DomainName, bool IsActive) : ICommand<OperationResult>;

public sealed class UpdateDomainHandler : ICommandHandler<UpdateDomainCommand, OperationResult>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(UpdateDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = await _domains.GetForAdminAsync(command.DomainId);
        if (domain == null)
            return DomainRules.NotFound();

        var renamed = domain.Rename(command.DomainName);
        if (!renamed.Succeeded)
            return OperationResult.From(renamed);

        domain.SetActive(command.IsActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza domeniul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateDomainCommand(int DomainId) : ICommand<OperationResult>;

public sealed class DeactivateDomainHandler : ICommandHandler<DeactivateDomainCommand, OperationResult>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(DeactivateDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = await _domains.GetForAdminAsync(command.DomainId);
        if (domain == null)
            return DomainRules.NotFound();

        domain.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

internal static class DomainRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Domeniul nu exista.");
}

internal static class DomainMapping
{
    public static DomainDto ToDto(TrainingDomain domain)
        => new(domain.DomainId, domain.DomainName, domain.IsActive);
}

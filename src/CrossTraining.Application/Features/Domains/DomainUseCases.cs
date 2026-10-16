using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using FluentValidation;

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

public sealed record CreateDomainCommand(string DomainName, bool IsActive) : ICommand<int>;

public sealed class CreateDomainValidator : AbstractValidator<CreateDomainCommand>
{
    public CreateDomainValidator() => RuleFor(c => c.DomainName).NotEmpty().MaximumLength(150);
}

public sealed class CreateDomainHandler : ICommandHandler<CreateDomainCommand, int>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = new TrainingDomain
        {
            DomainName = command.DomainName,
            IsActive = command.IsActive
        };

        _domains.Add(domain);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return domain.DomainId;
    }
}

public sealed record UpdateDomainCommand(int DomainId, string DomainName, bool IsActive) : ICommand<bool>;

public sealed class UpdateDomainValidator : AbstractValidator<UpdateDomainCommand>
{
    public UpdateDomainValidator()
    {
        RuleFor(c => c.DomainId).GreaterThan(0);
        RuleFor(c => c.DomainName).NotEmpty().MaximumLength(150);
    }
}

public sealed class UpdateDomainHandler : ICommandHandler<UpdateDomainCommand, bool>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = await _domains.GetForAdminAsync(command.DomainId);
        if (domain == null)
            return false;

        domain.DomainName = command.DomainName;
        domain.IsActive = command.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Dezactiveaza domeniul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateDomainCommand(int DomainId) : ICommand<bool>;

public sealed class DeactivateDomainValidator : AbstractValidator<DeactivateDomainCommand>
{
    public DeactivateDomainValidator() => RuleFor(c => c.DomainId).GreaterThan(0);
}

public sealed class DeactivateDomainHandler : ICommandHandler<DeactivateDomainCommand, bool>
{
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateDomainHandler(IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = await _domains.GetForAdminAsync(command.DomainId);
        if (domain == null)
            return false;

        domain.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class DomainMapping
{
    public static DomainDto ToDto(TrainingDomain domain)
        => new(domain.DomainId, domain.DomainName, domain.IsActive);
}

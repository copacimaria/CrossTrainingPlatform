using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.ViewModels;

/// <summary>
/// Un utilizator poate avea mai multe domenii, deci lucram cu o lista de id-uri selectate.
/// </summary>
public class ChangeDomainViewModel
{
    public List<int> SelectedDomainIds { get; set; } = new();
    public List<TrainingDomain> AvailableDomains { get; set; } = new();
}

using CrossTraining.Application.DTOs;

namespace CrossTraining.Application.ViewModels;

public class SearchClaimRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public List<ClaimDto> Claims { get; set; } = new();
}

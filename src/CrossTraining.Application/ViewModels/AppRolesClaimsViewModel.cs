using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using X.PagedList;

namespace CrossTraining.Application.ViewModels;

public class AppRolesClaimsViewModel
{
    public AppRole Role { get; set; } = new();
    public IPagedList<AppClaim>? Claims { get; set; }
    public List<ClaimDto> NormalClaims { get; set; } = new();
}

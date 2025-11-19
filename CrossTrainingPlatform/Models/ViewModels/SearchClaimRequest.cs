using CrossTrainingPlatform.Models.DTOs;

namespace CrossTrainingPlatform.Models.ViewModels
{
    public class SearchClaimRequest
    {
        public string SearchTerm { get; set; }
        public List<ClaimDto> Claims { get; set; }
    }
}

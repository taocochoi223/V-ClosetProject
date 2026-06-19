using System.Collections.Generic;

namespace VCloset.Application.DTOs.Admin.Responses;

public class OnboardingDemographicsDto
{
    public int TotalCompletedOnboarding { get; set; }
    public Dictionary<string, int> Lifestyles { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> EyeColors { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> HairColors { get; set; } = new Dictionary<string, int>();
    
    // Optional demographics
    public Dictionary<string, int> Genders { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> AgeGroups { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> Countries { get; set; } = new Dictionary<string, int>();
}

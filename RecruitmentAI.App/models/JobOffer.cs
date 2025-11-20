using System.Collections.Generic;

namespace RecruitmentAI.App.Models
{
    public class JobOffer
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string RequiredEnglishLevel { get; set; } = ""; // A1, A2, B1, B2, C1, C2
        public string RequiredEducation { get; set; } = ""; // Graduate, Undergraduate, Any
        public string MilitaryRequirement { get; set; } = ""; // Completed, Exempted, Any
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public double Salary { get; set; }
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; } // Higher number = higher priority
        public bool IsActive { get; set; } = true;
    }
}
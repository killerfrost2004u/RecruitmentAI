using System;

namespace RecruitmentAI.App.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        
        // Quadruple Name
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FatherName { get; set; }
        public string FamilyName { get; set; }
        
        // Contact Information
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int Age { get; set; }
        
        // Education & Status
        public string EducationStatus { get; set; } // Graduate/Undergraduate
        public string MilitaryStatus { get; set; }
        
        // Agency Information
        public string RecruiterName { get; set; }
        public string LeaderName { get; set; }
        public string UnitManagerName { get; set; }
        
        // Assessment Data
        public string VoiceNotePath { get; set; }
        public string EnglishLevel { get; set; } // A1, A2, B1, B2, C1, C2
        public string MatchedOffers { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } // New, Processed, Interview Scheduled
        
        public string FullName => $"{FirstName} {MiddleName} {FatherName} {FamilyName}";
    }
}
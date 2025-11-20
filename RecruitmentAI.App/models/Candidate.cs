using System;

namespace RecruitmentAI.App.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int Age { get; set; }
        public string? EducationStatus { get; set; }
        public string? MilitaryStatus { get; set; }
        public string? RecruiterName { get; set; }
        public string? LeaderName { get; set; }
        public string? UnitManagerName { get; set; }
        public string? VoiceNotePath { get; set; }
        public string? EnglishLevel { get; set; }
        public string? MatchedOffers { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Status { get; set; }
    }
}
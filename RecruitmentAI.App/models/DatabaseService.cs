using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecruitmentAI.App.Models
{
    public class DatabaseService
    {
        private string _connectionString;
        private string _databasePath;

        public DatabaseService()
        {
            _databasePath = Path.Combine(Directory.GetCurrentDirectory(), "recruitment.db");
            _connectionString = $"Data Source={_databasePath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Candidates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    MiddleName TEXT,
                    FatherName TEXT,
                    FamilyName TEXT,
                    Email TEXT,
                    PhoneNumber TEXT,
                    Age INTEGER,
                    EducationStatus TEXT,
                    MilitaryStatus TEXT,
                    RecruiterName TEXT,
                    LeaderName TEXT,
                    UnitManagerName TEXT,
                    VoiceNotePath TEXT,
                    EnglishLevel TEXT,
                    MatchedOffers TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Status TEXT DEFAULT 'New'
                );
            ";
            command.ExecuteNonQuery();
        }

        public void AddCandidate(Candidate candidate)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Candidates (
                    FirstName, MiddleName, FatherName, FamilyName, 
                    Email, PhoneNumber, Age, EducationStatus, MilitaryStatus,
                    RecruiterName, LeaderName, UnitManagerName, VoiceNotePath,
                    EnglishLevel, MatchedOffers, Status
                ) VALUES (
                    $firstName, $middleName, $fatherName, $familyName,
                    $email, $phoneNumber, $age, $educationStatus, $militaryStatus,
                    $recruiterName, $leaderName, $unitManagerName, $voiceNotePath,
                    $englishLevel, $matchedOffers, $status
                )
            ";

            // Add parameters to prevent SQL injection
            command.Parameters.AddWithValue("$firstName", candidate.FirstName);
            command.Parameters.AddWithValue("$middleName", candidate.MiddleName ?? "");
            command.Parameters.AddWithValue("$fatherName", candidate.FatherName ?? "");
            command.Parameters.AddWithValue("$familyName", candidate.FamilyName ?? "");
            command.Parameters.AddWithValue("$email", candidate.Email ?? "");
            command.Parameters.AddWithValue("$phoneNumber", candidate.PhoneNumber ?? "");
            command.Parameters.AddWithValue("$age", candidate.Age);
            command.Parameters.AddWithValue("$educationStatus", candidate.EducationStatus ?? "");
            command.Parameters.AddWithValue("$militaryStatus", candidate.MilitaryStatus ?? "");
            command.Parameters.AddWithValue("$recruiterName", candidate.RecruiterName ?? "");
            command.Parameters.AddWithValue("$leaderName", candidate.LeaderName ?? "");
            command.Parameters.AddWithValue("$unitManagerName", candidate.UnitManagerName ?? "");
            command.Parameters.AddWithValue("$voiceNotePath", candidate.VoiceNotePath ?? "");
            command.Parameters.AddWithValue("$englishLevel", candidate.EnglishLevel ?? "");
            command.Parameters.AddWithValue("$matchedOffers", candidate.MatchedOffers ?? "");
            command.Parameters.AddWithValue("$status", candidate.Status ?? "New");

            command.ExecuteNonQuery();
        }

        public List<Candidate> GetAllCandidates()
        {
            var candidates = new List<Candidate>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Candidates ORDER BY CreatedDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                candidates.Add(new Candidate
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    MiddleName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    FatherName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    FamilyName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    PhoneNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Age = reader.GetInt32(7),
                    EducationStatus = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    MilitaryStatus = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    RecruiterName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    LeaderName = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    UnitManagerName = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    VoiceNotePath = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    EnglishLevel = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    MatchedOffers = reader.IsDBNull(15) ? "" : reader.GetString(15),
                    CreatedDate = reader.GetDateTime(16),
                    Status = reader.IsDBNull(17) ? "New" : reader.GetString(17)
                });
            }

            return candidates;
        }
    }
}
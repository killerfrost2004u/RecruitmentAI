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
            UpdateDatabaseSchema();
            InitializeJobOffersTable(); // ← ADD THIS LINE
            AddSampleJobOffers(); // ← ADD THIS LINE (optional)
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

        private void UpdateDatabaseSchema()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if we need to update the schema
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info(Candidates)";

            var columns = new List<string>();
            using (var reader = checkCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1)); // Column name is at index 1
                }
            }

            // If old columns exist, we need to migrate
            if (columns.Contains("FirstName"))
            {
                // Create temporary table with new schema
                var migrateCommand = connection.CreateCommand();
                migrateCommand.CommandText = @"
                    CREATE TABLE Candidates_New (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName TEXT NOT NULL,
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

                    INSERT INTO Candidates_New (
                        Id, FullName, Email, PhoneNumber, Age, EducationStatus, MilitaryStatus,
                        RecruiterName, LeaderName, UnitManagerName, VoiceNotePath,
                        EnglishLevel, MatchedOffers, CreatedDate, Status
                    )
                    SELECT 
                        Id, 
                        FirstName || ' ' || COALESCE(MiddleName, '') || ' ' || COALESCE(FatherName, '') || ' ' || COALESCE(FamilyName, ''),
                        Email, PhoneNumber, Age, EducationStatus, MilitaryStatus,
                        RecruiterName, LeaderName, UnitManagerName, VoiceNotePath,
                        EnglishLevel, MatchedOffers, CreatedDate, Status
                    FROM Candidates;

                    DROP TABLE Candidates;
                    ALTER TABLE Candidates_New RENAME TO Candidates;
                ";
                migrateCommand.ExecuteNonQuery();
            }
        }

        public void AddCandidate(Candidate candidate)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Candidates (
                    FullName, 
                    Email, PhoneNumber, Age, EducationStatus, MilitaryStatus,
                    RecruiterName, LeaderName, UnitManagerName, VoiceNotePath,
                    EnglishLevel, MatchedOffers, Status
                ) VALUES (
                    $fullName,
                    $email, $phoneNumber, $age, $educationStatus, $militaryStatus,
                    $recruiterName, $leaderName, $unitManagerName, $voiceNotePath,
                    $englishLevel, $matchedOffers, $status
                )
            ";

            // Add parameters to prevent SQL injection
            command.Parameters.AddWithValue("$fullName", candidate.FullName ?? "");
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

        public void UpdateCandidateAssessment(int candidateId, string englishLevel, string matchedOffers, string status = "Assessed")
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Candidates 
                SET EnglishLevel = $englishLevel, 
                    MatchedOffers = $matchedOffers,
                    Status = $status
                WHERE Id = $id
            ";

            command.Parameters.AddWithValue("$englishLevel", englishLevel);
            command.Parameters.AddWithValue("$matchedOffers", matchedOffers);
            command.Parameters.AddWithValue("$status", status);
            command.Parameters.AddWithValue("$id", candidateId);

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
                    FullName = reader.IsDBNull(1) ? "" : reader.GetString(1), // Now reading from column 1
                    Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    PhoneNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Age = reader.GetInt32(4),
                    EducationStatus = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    MilitaryStatus = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    RecruiterName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    LeaderName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    UnitManagerName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    VoiceNotePath = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    EnglishLevel = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    MatchedOffers = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    CreatedDate = reader.GetDateTime(13),
                    Status = reader.IsDBNull(14) ? "New" : reader.GetString(14)
                });
            }

            return candidates;
        }
        // Add to DatabaseService class
        public void InitializeJobOffersTable()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS JobOffers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Company TEXT NOT NULL,
                    RequiredEnglishLevel TEXT NOT NULL,
                    RequiredEducation TEXT NOT NULL,
                    MilitaryRequirement TEXT NOT NULL,
                    MinAge INTEGER DEFAULT 18,
                    MaxAge INTEGER DEFAULT 60,
                    Salary REAL DEFAULT 0,
                    Location TEXT,
                    Description TEXT,
                    Priority INTEGER DEFAULT 1,
                    IsActive BOOLEAN DEFAULT 1
                );
            ";
            command.ExecuteNonQuery();
        }

        public void AddJobOffer(JobOffer job)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO JobOffers (
                    Title, Company, RequiredEnglishLevel, RequiredEducation, MilitaryRequirement,
                    MinAge, MaxAge, Salary, Location, Description, Priority, IsActive
                ) VALUES (
                    $title, $company, $englishLevel, $education, $military,
                    $minAge, $maxAge, $salary, $location, $description, $priority, $isActive
                )
            ";

            command.Parameters.AddWithValue("$title", job.Title);
            command.Parameters.AddWithValue("$company", job.Company);
            command.Parameters.AddWithValue("$englishLevel", job.RequiredEnglishLevel);
            command.Parameters.AddWithValue("$education", job.RequiredEducation);
            command.Parameters.AddWithValue("$military", job.MilitaryRequirement);
            command.Parameters.AddWithValue("$minAge", job.MinAge);
            command.Parameters.AddWithValue("$maxAge", job.MaxAge);
            command.Parameters.AddWithValue("$salary", job.Salary);
            command.Parameters.AddWithValue("$location", job.Location);
            command.Parameters.AddWithValue("$description", job.Description);
            command.Parameters.AddWithValue("$priority", job.Priority);
            command.Parameters.AddWithValue("$isActive", job.IsActive);

            command.ExecuteNonQuery();
        }

        public List<JobOffer> GetAllJobOffers()
        {
            var jobs = new List<JobOffer>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM JobOffers WHERE IsActive = 1 ORDER BY Priority DESC, Salary DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                jobs.Add(new JobOffer
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Company = reader.GetString(2),
                    RequiredEnglishLevel = reader.GetString(3),
                    RequiredEducation = reader.GetString(4),
                    MilitaryRequirement = reader.GetString(5),
                    MinAge = reader.GetInt32(6),
                    MaxAge = reader.GetInt32(7),
                    Salary = reader.GetDouble(8),
                    Location = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Description = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    Priority = reader.GetInt32(11),
                    IsActive = reader.GetBoolean(12)
                });
            }

            return jobs;
        }
        private void AddSampleJobOffers()
        {
            var existingJobs = GetAllJobOffers();
            if (existingJobs.Count == 0)
            {
                // Add sample job offers
                var sampleJobs = new List<JobOffer>
                {
                    new JobOffer { 
                        Title = "Customer Service Representative", 
                        Company = "Your Company", 
                        RequiredEnglishLevel = "B1", 
                        RequiredEducation = "Any",
                        MilitaryRequirement = "Any",
                        MinAge = 20, 
                        MaxAge = 45,
                        Salary = 8000,
                        Location = "Cairo",
                        Priority = 1
                    },
                    new JobOffer { 
                        Title = "Team Leader", 
                        Company = "Your Company", 
                        RequiredEnglishLevel = "B2", 
                        RequiredEducation = "Graduate",
                        MilitaryRequirement = "Completed",
                        MinAge = 25, 
                        MaxAge = 40,
                        Salary = 12000,
                        Location = "Alexandria", 
                        Priority = 2
                    },
                    new JobOffer { 
                        Title = "Sales Manager", 
                        Company = "Your Company", 
                        RequiredEnglishLevel = "C1", 
                        RequiredEducation = "Graduate",
                        MilitaryRequirement = "Completed", 
                        MinAge = 28,
                        MaxAge = 45,
                        Salary = 18000,
                        Location = "Cairo",
                        Priority = 3
                    }
                };

                foreach (var job in sampleJobs)
                {
                    AddJobOffer(job);
                }
            }
        }
    }
}
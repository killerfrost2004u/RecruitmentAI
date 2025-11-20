using System;
using System.IO;
using System.Speech.Recognition;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecruitmentAI.App.Models
{
    public class ManualAssessment
    {
        public int CandidateId { get; set; }
        public string ExpertEnglishLevel { get; set; } = "";
        public string ExpertFeedback { get; set; } = "";
        public DateTime AssessmentDate { get; set; }
    }
    public class MLAssessmentResult
    {
        public string Level { get; set; } = "B1";
        public double Confidence { get; set; } = 0.85;
        public Dictionary<string, double> DetailedScores { get; set; } = new Dictionary<string, double>();
    }

    public class EnglishAssessmentService
    {
        private List<ManualAssessment> _manualAssessments = new List<ManualAssessment>();

        public void SaveManualAssessment(int candidateId, string englishLevel, string feedback)
        {
            _manualAssessments.Add(new ManualAssessment
            {
                CandidateId = candidateId,
                ExpertEnglishLevel = englishLevel,
                ExpertFeedback = feedback,
                AssessmentDate = DateTime.Now
            });

            SaveTrainingDataToFile();
        }

        private void SaveTrainingDataToFile()
        {
            try
            {
                var trainingData = _manualAssessments.Select(ma => new
                {
                    ma.CandidateId,
                    ma.ExpertEnglishLevel,
                    ma.ExpertFeedback,
                    ma.AssessmentDate
                }).ToList();
                
                var json = System.Text.Json.JsonSerializer.Serialize(trainingData, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                File.WriteAllText("training_data.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving training data: {ex.Message}");
            }
        }
        
        public class AssessmentResult
        {
            public string EnglishLevel { get; set; } = "Pending";
            public double ConfidenceScore { get; set; }
            public string Feedback { get; set; } = "Assessment in progress";
            public List<string> MatchedJobs { get; set; } = new List<string>();
            public Dictionary<string, double> FeatureScores { get; set; } = new Dictionary<string, double>();
        }

        // Job requirements by English level
        private readonly Dictionary<string, List<string>> _jobRequirements = new()
        {
            ["A1"] = new List<string> { "Basic Labor", "Entry-Level Manual Work" },
            ["A2"] = new List<string> { "Customer Service Basic", "Retail Assistant", "Housekeeping" },
            ["B1"] = new List<string> { "Team Leader", "Supervisor", "Technical Support", "Hospitality Staff" },
            ["B2"] = new List<string> { "Manager", "Sales Executive", "IT Support", "Admin Supervisor" },
            ["C1"] = new List<string> { "Senior Manager", "Project Lead", "Client Relations", "Training Specialist" },
            ["C2"] = new List<string> { "Executive", "Director", "Consultant", "International Relations" }
        };

        private List<string> MatchJobsToLevel(string englishLevel)
        {
            return _jobRequirements.ContainsKey(englishLevel) 
                ? _jobRequirements[englishLevel] 
                : new List<string> { "General Positions" };
        }

        // ADD THIS METHOD TO YOUR EnglishAssessmentService CLASS
        public AssessmentResult AssessEnglishLevel(string audioFilePath, string transcription = "")
        {
            // If no transcription provided, try to transcribe
            if (string.IsNullOrEmpty(transcription))
            {
                transcription = TranscribeAudio(audioFilePath);
            }

            var features = AnalyzeSpeechFeatures(transcription, audioFilePath);
            var level = CalculateCEFRLevel(features);
            var jobs = MatchJobsToLevel(level); // Use the SIMPLE version

            return new AssessmentResult
            {
                EnglishLevel = level,
                ConfidenceScore = features.OverallScore,
                Feedback = GenerateFeedback(level, features),
                MatchedJobs = jobs,
                FeatureScores = new Dictionary<string, double>
                {
                    ["Vocabulary"] = features.VocabularyScore,
                    ["Grammar"] = features.GrammarScore,
                    ["Fluency"] = features.FluencyScore,
                    ["Content"] = features.ContentScore
                }
            };
        }

        public AssessmentResult AssessEnglishLevelWithJobMatching(string audioFilePath, Candidate candidate, List<JobOffer> availableJobs, string transcription = "")
        {
            // If no transcription provided, try to transcribe
            if (string.IsNullOrEmpty(transcription))
            {
                transcription = TranscribeAudio(audioFilePath);
            }

            var features = AnalyzeSpeechFeatures(transcription, audioFilePath);
            var level = CalculateCEFRLevel(features);
            var jobs = MatchJobsToLevel(level, candidate, availableJobs);

            return new AssessmentResult
            {
                EnglishLevel = level,
                ConfidenceScore = features.OverallScore,
                Feedback = GenerateFeedback(level, features),
                MatchedJobs = jobs,
                FeatureScores = new Dictionary<string, double>
                {
                    ["Vocabulary"] = features.VocabularyScore,
                    ["Grammar"] = features.GrammarScore,
                    ["Fluency"] = features.FluencyScore,
                    ["Content"] = features.ContentScore
                }
            };
        }

        private string TranscribeAudio(string audioFilePath)
        {
            try
            {
                // Simple transcription using System.Speech (works for WAV files)
                if (File.Exists(audioFilePath) && audioFilePath.EndsWith(".wav"))
                {
                    using (var speechRecognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US")))
                    {
                        speechRecognitionEngine.SetInputToWaveFile(audioFilePath);
                        var result = speechRecognitionEngine.Recognize();
                        return result?.Text ?? "Could not transcribe audio";
                    }
                }
                else
                {
                    // For other formats, return placeholder
                    return "Audio transcription requires WAV format or external service";
                }
            }
            catch (Exception)
            {
                return "Automatic transcription not available - using rule-based assessment";
            }
        }

        private SpeechFeatures AnalyzeSpeechFeatures(string transcription, string audioFilePath)
        {
            var features = new SpeechFeatures();

            // Analyze vocabulary complexity
            features.VocabularyScore = AnalyzeVocabulary(transcription);

            // Analyze grammar and sentence structure
            features.GrammarScore = AnalyzeGrammar(transcription);

            // Analyze fluency based on text characteristics
            features.FluencyScore = AnalyzeFluency(transcription);

            // Analyze content quality
            features.ContentScore = AnalyzeContent(transcription);

            // Calculate overall score
            features.OverallScore = (features.VocabularyScore + features.GrammarScore + 
                                   features.FluencyScore + features.ContentScore) / 4.0;

            return features;
        }

        private double AnalyzeVocabulary(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.1;

            var words = text.ToLower().Split(' ', '.', ',', '!', '?')
                           .Where(w => w.Length > 2)
                           .ToArray();

            if (words.Length == 0) return 0.1;

            // Calculate unique word ratio (lexical diversity)
            var uniqueWords = words.Distinct().Count();
            var diversityRatio = (double)uniqueWords / words.Length;

            // Score based on word length and diversity
            var avgWordLength = words.Average(w => w.Length);
            var score = (diversityRatio * 0.6) + (Math.Min(avgWordLength / 10.0, 1.0) * 0.4);

            return Math.Min(score, 1.0);
        }

        private double AnalyzeGrammar(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.1;

            var sentences = text.Split('.', '!', '?')
                               .Where(s => s.Trim().Length > 5)
                               .ToArray();

            if (sentences.Length == 0) return 0.3;

            // Simple grammar assessment based on sentence structure
            double score = 0.0;
            int validSentences = 0;

            foreach (var sentence in sentences)
            {
                var words = sentence.Trim().Split(' ');
                if (words.Length >= 3) // Basic sentence has subject + verb + object
                {
                    validSentences++;
                    // Longer sentences with proper structure get higher scores
                    score += Math.Min(words.Length / 15.0, 1.0);
                }
            }

            return validSentences > 0 ? Math.Min(score / validSentences, 1.0) : 0.3;
        }

        private double AnalyzeFluency(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.1;

            var words = text.Split(' ').Where(w => w.Length > 0).ToArray();
            if (words.Length < 10) return 0.3;

            // Fluency based on text length and structure
            var wordCount = words.Length;
            var sentenceCount = text.Count(c => c == '.') + text.Count(c => c == '!') + text.Count(c => c == '?');
            
            if (sentenceCount == 0) sentenceCount = 1;

            var wordsPerSentence = (double)wordCount / sentenceCount;
            
            // Ideal range: 10-20 words per sentence indicates good fluency
            var fluencyScore = wordsPerSentence switch
            {
                < 5 => 0.3,  // Too short
                >= 5 and <= 8 => 0.5, // Basic
                >= 9 and <= 15 => 0.7, // Good
                >= 16 and <= 25 => 0.8, // Very good
                > 25 => 0.6 // Too long, might be run-on sentences
            };

            return fluencyScore;
        }

        private double AnalyzeContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.1;

            // Score based on content length and coherence
            var wordCount = text.Split(' ').Count(w => w.Length > 0);
            var score = Math.Min(wordCount / 100.0, 1.0); // More content = better score

            return score;
        }

        private string CalculateCEFRLevel(SpeechFeatures features)
        {
            var overallScore = features.OverallScore;

            if (double.IsNaN(overallScore) || double.IsInfinity(overallScore))
                return "A1";

            return overallScore switch
            {
                < 0.2 => "A1",
                < 0.35 => "A2", 
                < 0.55 => "B1",
                < 0.75 => "B2",
                < 0.9 => "C1",
                _ => "C2"
            };
        }

        private List<string> MatchJobsToLevel(string englishLevel, Candidate candidate, List<JobOffer> availableJobs)
        {
            var matchedJobs = new List<string>();
            var candidateAge = candidate.Age;

            foreach (var job in availableJobs)
            {
                if (IsCandidateQualified(candidate, job, englishLevel))
                {
                    // Calculate match score
                    var matchScore = CalculateMatchScore(candidate, job, englishLevel);
                    matchedJobs.Add($"{job.Title} at {job.Company} (${job.Salary}) - Score: {matchScore:P0}");
                }
            }

            // Return top 3 matches sorted by score
            return matchedJobs.OrderByDescending(j => j).Take(3).ToList();
        }

        private bool IsCandidateQualified(Candidate candidate, JobOffer job, string englishLevel)
        {
            // English level qualification (C2 > C1 > B2 > B1 > A2 > A1)
            var englishLevels = new Dictionary<string, int> { ["A1"] = 1, ["A2"] = 2, ["B1"] = 3, ["B2"] = 4, ["C1"] = 5, ["C2"] = 6 };

            if (englishLevels[englishLevel] < englishLevels[job.RequiredEnglishLevel])
                return false;

            // Age qualification
            if (candidate.Age < job.MinAge || candidate.Age > job.MaxAge)
                return false;

            // Education qualification
            if (job.RequiredEducation != "Any" && candidate.EducationStatus != job.RequiredEducation)
                return false;

            // Military qualification
            if (job.MilitaryRequirement != "Any" && candidate.MilitaryStatus != job.MilitaryRequirement)
                return false;

            return true;
        }

        private double CalculateMatchScore(Candidate candidate, JobOffer job, string englishLevel)
        {
            double score = 0.0;
            var englishLevels = new Dictionary<string, int> { ["A1"] = 1, ["A2"] = 2, ["B1"] = 3, ["B2"] = 4, ["C1"] = 5, ["C2"] = 6 };

            // English level match (closer to required level = higher score)
            var englishDiff = englishLevels[englishLevel] - englishLevels[job.RequiredEnglishLevel];
            score += Math.Max(0.5, 1.0 - (englishDiff * 0.1));

            // Age match (closer to middle of range = higher score)
            var ageRangeMiddle = (job.MinAge + job.MaxAge) / 2.0;
            var ageDiff = Math.Abs(candidate.Age - ageRangeMiddle);
            var ageRange = job.MaxAge - job.MinAge;
            score += Math.Max(0.3, 1.0 - (ageDiff / ageRange));

            // Priority bonus
            score += (job.Priority * 0.1);

            return Math.Min(score, 1.0);
        }

        private string GenerateFeedback(string level, SpeechFeatures features)
        {
            return level switch
            {
                "A1" => "Basic English communication. Focus on building vocabulary and simple sentences.",
                "A2" => "Elementary English. Practice common phrases and basic conversations.",
                "B1" => "Intermediate English. Work on fluency and more complex sentence structures.",
                "B2" => "Upper-intermediate English. Good communication skills, focus on nuance and professional vocabulary.",
                "C1" => "Advanced English. Excellent communication skills suitable for professional environments.",
                "C2" => "Proficient English. Near-native level suitable for executive roles.",
                _ => "Assessment completed."
            };
        }

         // ↓ REPLACE THIS ENTIRE METHOD ↓
        public async Task<AssessmentResult> AssessWithMLModel(string audioFilePath)
        {
            var mlClient = new MLApiClient();

            // Check if ML service is available
            var isAvailable = await mlClient.IsMLServiceAvailable();

            if (!isAvailable)
            {
                // Fallback to rule-based assessment
                return AssessEnglishLevel(audioFilePath);
            }

            try
            {
                // Call ML API
                var mlResult = await mlClient.AssessEnglishWithML(audioFilePath);

                if (mlResult.Success)
                {
                    return new AssessmentResult
                    {
                        EnglishLevel = mlResult.EnglishLevel,
                        ConfidenceScore = mlResult.Confidence,
                        Feedback = GenerateMLFeedback(mlResult.EnglishLevel, mlResult.Confidence),
                        MatchedJobs = MatchJobsToLevel(mlResult.EnglishLevel),
                        FeatureScores = new Dictionary<string, double>()
                    };
                }
                else
                {
                    // ML API failed, fallback to rule-based
                    return AssessEnglishLevel(audioFilePath);
                }
            }
            catch (Exception ex)
            {
                // Fallback to rule-based assessment
                Console.WriteLine($"ML assessment failed: {ex.Message}");
                return AssessEnglishLevel(audioFilePath);
            }
        }
        // ↓ REPLACE THIS METHOD ↓
        private string GenerateMLFeedback(string level, double confidence)
        {
            var confidenceText = confidence switch
            {
                > 0.9 => "very high confidence",
                > 0.7 => "high confidence", 
                > 0.5 => "moderate confidence",
                _ => "low confidence"
            };

            return $"ML Assessment ({confidenceText}): {GetDetailedFeedback(level)}";
        }

        // ↓ ADD THIS NEW METHOD AFTER GenerateMLFeedback ↓
        private string GetDetailedFeedback(string level)
        {
            return level switch
            {
                "A1" => "Basic user. Can understand and use familiar expressions. Focus on basic vocabulary and simple sentences.",
                "A2" => "Elementary user. Can communicate in routine tasks. Practice everyday expressions and basic grammar.",
                "B1" => "Intermediate user. Can handle most travel situations. Work on fluency and connecting phrases.",
                "B2" => "Upper-intermediate user. Can interact with native speakers. Focus on complex texts and professional vocabulary.",
                "C1" => "Advanced user. Can use language flexibly for social and professional purposes.",
                "C2" => "Proficient user. Can understand virtually everything heard or read. Near-native fluency.",
                _ => "Assessment completed with advanced AI analysis."
            };
        }
    }

    public class SpeechFeatures
    {
        public double VocabularyScore { get; set; }
        public double GrammarScore { get; set; }
        public double FluencyScore { get; set; }
        public double ContentScore { get; set; }
        public double OverallScore { get; set; }
    }
}
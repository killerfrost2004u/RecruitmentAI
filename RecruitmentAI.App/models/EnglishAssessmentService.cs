using System;
using System.IO;
using System.Speech.Recognition;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace RecruitmentAI.App.Models
{
    public class EnglishAssessmentService
    {
        public class AssessmentResult
        {
            public string EnglishLevel { get; set; } // A1, A2, B1, B2, C1, C2
            public double ConfidenceScore { get; set; }
            public string Feedback { get; set; }
            public List<string> MatchedJobs { get; set; }
            public Dictionary<string, double> FeatureScores { get; set; }
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

        public AssessmentResult AssessEnglishLevel(string audioFilePath, string transcription = "")
        {
            // If no transcription provided, try to transcribe
            if (string.IsNullOrEmpty(transcription))
            {
                transcription = TranscribeAudio(audioFilePath);
            }

            var features = AnalyzeSpeechFeatures(transcription, audioFilePath);
            var level = CalculateCEFRLevel(features);
            var jobs = MatchJobsToLevel(level);

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

        private List<string> MatchJobsToLevel(string englishLevel)
        {
            return _jobRequirements.ContainsKey(englishLevel) 
                ? _jobRequirements[englishLevel] 
                : new List<string> { "General Positions" };
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
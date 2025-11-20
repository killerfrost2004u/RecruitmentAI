using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecruitmentAI.App.Models
{
    public class MLApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:8000";

        public MLApiClient()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public class MLAssessmentResponse
        {
            public bool Success { get; set; }
            public string EnglishLevel { get; set; } = "B1";
            public double Confidence { get; set; }
            public string ModelUsed { get; set; } = "fallback";
            public string Error { get; set; } = "";
        }

        public async Task<MLAssessmentResponse> AssessEnglishWithML(string audioFilePath)
        {
            try
            {
                if (!File.Exists(audioFilePath))
                {
                    return new MLAssessmentResponse { Success = false, Error = "Audio file not found" };
                }

                // Read audio file
                var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
                
                // Create multipart form data
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(audioBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(fileContent, "file", Path.GetFileName(audioFilePath));

                // Send to ML API
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/assess-english", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MLAssessmentResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return result ?? new MLAssessmentResponse { Success = false, Error = "Invalid response" };
                }
                else
                {
                    return new MLAssessmentResponse { Success = false, Error = $"API error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new MLAssessmentResponse { Success = false, Error = ex.Message };
            }
        }

        public async Task<bool> IsMLServiceAvailable()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
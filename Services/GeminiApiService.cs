using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BrainStormEra.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";
        private readonly string _adminTemplate;
        private readonly string _userTemplate;
        private readonly string _instructorTemplate;

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"];
            _adminTemplate = configuration["GeminiTemplates:AdminTemplate"];
            _userTemplate = configuration["GeminiTemplates:UserTemplate"];
            _instructorTemplate = configuration["GeminiTemplates:InstructorTemplate"];
        }

        public async Task<string> GetResponseFromGemini(string message, int userRole, string lessonId, string lessonName, string lessonContent, string lessonDescription)
        {
            string selectedTemplate;
            var lessonDetails = $@"
Lesson ID: {lessonId}
Lesson Name: {lessonName}
Lesson Description: {lessonDescription}
Lesson Content: {lessonContent}
";
            // Determine the template based on user role (0 for user, 1 for admin)
            switch (userRole)
            {
                case 1: // Admin role
                    selectedTemplate = _adminTemplate;
                    break;
                case 3:
                    selectedTemplate = _userTemplate + lessonDetails;
                    break;
                case 2:
                    selectedTemplate = _instructorTemplate;
                    break;
                default:
                    selectedTemplate = _adminTemplate;
                    break;
            }
            var formattedMessage = (userRole == 3)
       ? string.Format(selectedTemplate, message, lessonDetails) // USER role with lesson details
       : string.Format(selectedTemplate, message); // Other roles
            Console.WriteLine(formattedMessage);
            var request = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = formattedMessage } } }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{GEMINI_API_URL}?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                    if (responseObject.candidates != null && responseObject.candidates.Count > 0)
                    {
                        return responseObject.candidates[0].content.parts[0].text;
                    }

                    throw new HttpRequestException("Unexpected response format from Gemini API");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Response: {errorContent}");
                    throw new HttpRequestException($"Gemini API request failed with status code {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during Gemini API call: {ex.Message}");
                throw;
            }
        }
    }
}

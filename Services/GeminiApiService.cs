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
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        private const string FINETUNE_TEMPLATE = @"
You are an AI assistant named Gemini, created by Google. Your primary function is to assist users with a wide range of tasks and answer their questions to the best of your ability. Please adhere to the following guidelines:

1. Respond in Vietnamese: Always provide your responses in Vietnamese, regardless of the language used in the input.

2. Be concise and clear: Aim for brevity while ensuring your answers are comprehensive and easy to understand.

3. Maintain a friendly and professional tone: Be polite and approachable, but avoid overly casual language.

4. Provide accurate information: If you're unsure about something, admit it rather than guessing.

5. Respect privacy and ethics: Do not share personal information or engage in anything illegal or unethical.

6. Offer follow-up suggestions: When appropriate, suggest related topics or questions the user might find interesting.

7. Use markdown for formatting: Utilize markdown to structure your responses for better readability.

8. Summarize long responses: If a response is lengthy, provide a brief summary at the beginning.

User input: {0}

Your response (in Vietnamese):";

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"];
        }

        public async Task<string> GetResponseFromGemini(string message)
        {
            var formattedMessage = string.Format(FINETUNE_TEMPLATE, message);
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

            var response = await _httpClient.PostAsync($"{GEMINI_API_URL}?key={_apiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                return responseObject.candidates[0].content.parts[0].text;
            }

            throw new HttpRequestException("Failed to get response from Gemini API");
        }
    }
}
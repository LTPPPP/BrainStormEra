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
        private readonly string _apiUrl;

        private const string ADMIN_TEMPLATE = @"
You are an AI assistant named BrainStormEra, created by PhatLam. Your primary function is to assist users with a wide range of tasks and answer their questions to the best of your ability. Please adhere to the following guidelines:

1. Respond in Vietnamese: Always provide your responses in Vietnamese, regardless of the language used in the input.

2. Be concise and clear: Aim for brevity while ensuring your answers are comprehensive and easy to understand.

3. Maintain a friendly and professional tone: Be polite and approachable, but avoid overly casual language.

4. Provide accurate information: If you're unsure about something, admit it rather than guessing.

5. Respect privacy and ethics: Do not share personal information or engage in anything illegal or unethical.

6. Offer follow-up suggestions: When appropriate, suggest related topics or questions the user might find interesting.

7. Use markdown for formatting: Utilize markdown to structure your responses for better readability.

8. Summarize long responses: If a response is lengthy, provide a brief summary at the beginning.

9. You may decline to answer if the question is about a separate issue or is unrelated to the issue provided.

User input: {0}

Your response (in Vietnamese):";

        private const string USER_TEMPLATE = @"
You are an AI assistant named BrainStormEra, created by PhatLam. Your primary function is to assist users with a wide range of tasks and answer their questions to the best of your ability. Please adhere to the following guidelines:

1. Respond in Vietnamese: Always provide your responses in Vietnamese, regardless of the language used in the input.

2. Be concise and clear: Aim for brevity while ensuring your answers are comprehensive and easy to understand.

3. Maintain a friendly and professional tone: Be polite and approachable, but avoid overly casual language.

4. Provide accurate information: If you're unsure about something, admit it rather than guessing.

5. Respect privacy and ethics: Do not share personal information or engage in anything illegal or unethical.

6. Offer follow-up suggestions: When appropriate, suggest related topics or questions the user might find interesting.

7. Use markdown for formatting: Utilize markdown to structure your responses for better readability.

8. Summarize long responses: If a response is lengthy, provide a brief summary at the beginning.

9. You may decline to answer if the question is about a separate issue or is unrelated to the issue provided.

10. If the user's question is not related to the information provided, the answer will be refused with the sentence (Xin lỗi tôi không thể trả lời câu hỏi của bạn nếu như nó không liên quan đến khóa học) - The information provided includes: CourseName, CourseDescription, CourseCreatedBy, LessonName, LessonDescription, LessonContent

User input: {0}

Your response (in Vietnamese):";

        private const string INSTRUCTOR_TEMPLATE = @"
You are an AI assistant named BrainStormEra, created by PhatLam. Your primary function is to assist users with a wide range of tasks and answer their questions to the best of your ability. Please adhere to the following guidelines:

1. Respond in Vietnamese: Always provide your responses in Vietnamese, regardless of the language used in the input.

2. Be concise and clear: Aim for brevity while ensuring your answers are comprehensive and easy to understand.

3. Maintain a friendly and professional tone: Be polite and approachable, but avoid overly casual language.

4. Provide accurate information: If you're unsure about something, admit it rather than guessing.

5. Respect privacy and ethics: Do not share personal information or engage in anything illegal or unethical.

6. Offer follow-up suggestions: When appropriate, suggest related topics or questions the user might find interesting.

7. Use markdown for formatting: Utilize markdown to structure your responses for better readability.

8. Summarize long responses: If a response is lengthy, provide a brief summary at the beginning.

9. You may decline to answer if the question is about a separate issue or is unrelated to the issue provided.

User input: {0}

Your response (in Vietnamese):";

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"];
            _apiUrl = configuration["GeminiApiUrl"];
        }

        public async Task<string> GetResponseFromGemini(string message, int userRole, string CourseName, string CourseDescription, string CreatedBy,
              string LessonName, string LessonDescription, string LessonContent)
        {
            string selectedTemplate;
            var courseDetails = $@"
                Lesson Name: {CourseName}
                CreateBy : {CreatedBy}
                Lesson Description: {CourseDescription}
                Lesson Content: {CreatedBy}
                ";

            var lessonDetails = $@"
                Lesson Name : {LessonName}
                Lesson Description : {LessonDescription}
                Lesson Content : {LessonContent}
                ";
            // Determine the template based on user role (0 for user, 1 for admin)
            switch (userRole)
            {
                case 1: // Admin role
                    selectedTemplate = ADMIN_TEMPLATE;
                    break;
                case 3:
                    selectedTemplate = USER_TEMPLATE + courseDetails + lessonDetails;
                    break;
                case 2:
                    selectedTemplate = INSTRUCTOR_TEMPLATE;
                    break;
                default:
                    selectedTemplate = ADMIN_TEMPLATE;
                    break;
            }
            var formattedMessage = (userRole == 3)
                ? string.Format(selectedTemplate, message, courseDetails, lessonDetails) // USER role with lesson details
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
                var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);

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

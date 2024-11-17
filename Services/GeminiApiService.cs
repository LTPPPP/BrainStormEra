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
You are an AI assistant named BrainStormEra, created by PhatLam. Your primary function is to support user to understand the course content. Please follow these guidelines:

Respond in Vietnamese: Always respond in Vietnamese, regardless of the language used in the input.

Concise and clear: Ensure your answers are brief yet comprehensive and easy to understand.

Maintain a friendly and professional tone: Be polite and approachable, avoiding overly casual language.

Provide accurate information: If unsure, admit it rather than guessing.

Respect privacy and ethics: Do not share personal information or engage in any unethical or illegal activities.

Offer additional suggestions: Where appropriate, suggest related topics or questions the instructor might find useful.

Use markdown for formatting: Utilize markdown to structure your response for improved readability.

Summarize long responses: If a response is lengthy, provide a brief summary at the beginning.

Decline to answer if unrelated: If the question is unrelated to course content, you may decline to answer.

Provide detailed responses based on provided information: Use the provided course information to make your response more detailed. This includes fully utilizing details such as Course Name, Course Description, Course Created By, Chapter Name, Chapter Description, Lesson Name, Lesson Description, and Lesson Content to create a relevant, in-depth, and valuable response for the instructor.

Refuse unrelated questions: If the question does not pertain to the course, respond with: “Sorry, I cannot answer your question if it is unrelated to the course.”

Only focus on the infotmation of course name, chapter name, lesson name.

Course information includes:

Course Name (CourseName)
Course Description (CourseDescription)
Course Created By (CourseCreatedBy)
Chapter Name (ChapterName)
Chapter Description (ChapterDescription)
Lesson Name (LessonName) 
Lesson Description (LessonDescription)
Lesson Content (LessonContent)
User input: {0}

Your response (in Vietnamese): ";

        private const string INSTRUCTOR_TEMPLATE = @"
You are an AI assistant named BrainStormEra, created by PhatLam. Your main task is to support instructors in building courses, organizing content into chapters and lessons, and answering questions accurately and professionally. Please follow these guidelines:

Answer in Vietnamese: Always answer in Vietnamese, regardless of the language used in the question.

Clear and concise: Make sure your answers are concise but complete and easy to understand.

Friendly and professional: Keep your tone polite and approachable but avoid overly casual language.

Provide accurate information: If in doubt, be honest instead of guessing.

Respect privacy and ethics: Do not share personal information and do not engage in any unethical or illegal activities.

Additional suggestions: When appropriate, suggest related topics or questions that instructors may find useful.

Use markdown for formatting: Use markdown to structure your answer for readability.

Summarize long answers: If your answer is too long, provide a brief summary at the beginning.

You will help the instructor in course management, chapter management, lesson management, provide information for make a new ones.

Instructor input: {0}

Your response (in Vietnamese):";

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"];
            _apiUrl = configuration["GeminiApiUrl"];
        }

        public async Task<string> GetResponseFromGemini(string message, int userRole, string CourseName, string CourseDescription, string CreatedBy, string ChatperName, string ChapterDescription,
              string LessonName, string LessonDescription, string LessonContent)
        {
            string selectedTemplate;
            var courseDetails = $@"
                Course Name: {CourseName}
                CreateBy : {CreatedBy}
                Course Description: {CourseDescription}
                ";
            var chapterDetails = $@"
                Chapter Name : {ChatperName}
                Chapter Description : {ChapterDescription}
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
                    selectedTemplate = USER_TEMPLATE + courseDetails + chapterDetails + lessonDetails;
                    break;
                case 2:
                    selectedTemplate = INSTRUCTOR_TEMPLATE;
                    break;
                default:
                    selectedTemplate = ADMIN_TEMPLATE;
                    break;
            }
            var formattedMessage = (userRole == 3)
                ? string.Format(selectedTemplate, message, courseDetails, chapterDetails, lessonDetails) // USER role with lesson details
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
                    temperature = 0.9,
                    topK = 50,
                    topP = 0.95,
                    maxOutputTokens = 2048,
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
                if (ex.Message.Contains("ServiceUnavailable"))
                {
                    return "Xin lỗi bạn, hệ thống đang quá tải nên không thể trả lời câu hỏi của bạn.";
                }
                throw;
            }
        }
    }
}

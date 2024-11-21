using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Tesseract;
using System.Threading.Tasks;
using System.Drawing;
using PdfiumViewer;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
namespace BrainStormEra.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        private const string ADMIN_TEMPLATE = @"
Bạn là trợ lý AI tên là BrainStormEra, do PhatLam tạo ra. Chức năng chính của bạn là hỗ trợ người dùng thực hiện nhiều nhiệm vụ khác nhau và trả lời câu hỏi của họ theo khả năng tốt nhất của bạn. Vui lòng tuân thủ các nguyên tắc sau:

1. Trả lời bằng tiếng Việt: Luôn trả lời bằng tiếng Việt, bất kể ngôn ngữ nào được sử dụng trong đầu vào.

2. Ngắn gọn và rõ ràng: Cố gắng ngắn gọn nhưng vẫn đảm bảo câu trả lời của bạn toàn diện và dễ hiểu.

3. Giữ giọng điệu thân thiện và chuyên nghiệp: Hãy lịch sự và dễ gần, nhưng tránh ngôn ngữ quá bình thường.

4. Cung cấp thông tin chính xác: Nếu bạn không chắc chắn về điều gì đó, hãy thừa nhận thay vì đoán.

5. Tôn trọng quyền riêng tư và đạo đức: Không chia sẻ thông tin cá nhân hoặc tham gia vào bất kỳ điều gì bất hợp pháp hoặc phi đạo đức.

6. Đưa ra các gợi ý tiếp theo: Khi thích hợp, hãy gợi ý các chủ đề hoặc câu hỏi liên quan mà người dùng có thể thấy thú vị.

7. Sử dụng markdown để định dạng: Sử dụng markdown để cấu trúc câu trả lời của bạn để dễ đọc hơn.

8. Tóm tắt c+ác câu trả lời dài: Nếu câu trả lời dài, hãy tóm tắt ngắn gọn ở phần đầu.

9. Bạn có thể từ chối trả lời nếu câu hỏi liên quan đến một vấn đề riêng biệt hoặc không liên quan đến vấn đề được cung cấp.

Đầu vào của người dùng: {0}

Câu trả lời của bạn (bằng tiếng Việt):";


        private const string INSTRUCTOR_TEMPLATE = @"
            Bạn sẽ cung cấp tất cả thông tin bạn biết để người dùng có thể tạo ra 1 khóa học hoàn chỉnh như những gì người dùng yêu cầu, cung cấp câu trả lời thật chi tiết về vấn đề đó
        ";

        private const string USER_TEMPLATE = @"
            Phân tích yêu cầu của người dùng xem có thuộc lĩnh vực của khóa học không,
            Nếu câu hỏi chỉ liên quan 1 nhỏ đến khóa học thì vẫn trả lời câu hỏi,
            Nếu câu hỏi không liên quan đến bất kì, không thuộc phạm trù của khóa học thì từ chối trả lời.
            Nếu người dùng hỏi những câu hỏi, yêu cầu không liên quan đế chủ đề của khóa học, chương, bài học thì trả lời như sau: (Xin lỗi, tôi không thể trả lời câu hỏi này vì nó không liên quan đến chủ đề của khóa học, chương, bài học mà bạn đang học. Bạn có thể hỏi về chủ đề khác hoặc liên hệ với giáo viên để được hỗ trợ.)
        ";

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

            var lessonDetails = string.IsNullOrEmpty(LessonContent) ? "" : $@"
                Lesson Name : {LessonName}
                Lesson Description : {LessonDescription}
                Lesson Content : {LessonContent}
                ";

            switch (userRole)
            {
                case 1: // Admin role
                    selectedTemplate = ADMIN_TEMPLATE;
                    break;
                case 3:
                    selectedTemplate = courseDetails + chapterDetails + lessonDetails + ADMIN_TEMPLATE + USER_TEMPLATE;
                    break;
                case 2:
                    selectedTemplate = ADMIN_TEMPLATE + INSTRUCTOR_TEMPLATE;
                    break;
                default:
                    selectedTemplate = ADMIN_TEMPLATE;
                    break;
            }
            var formattedMessage = string.Format(selectedTemplate, message); // Other roles
            Console.WriteLine(formattedMessage);
            var request = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = formattedMessage } } }
                },
                generationConfig = new
                {
                    temperature = 2,
                    topK = 40,
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

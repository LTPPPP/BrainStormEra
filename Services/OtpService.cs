namespace BrainStormEra.Services
{
    public class OtpService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OtpService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GenerateOtp(string email)
        {
            var otpCode = new Random().Next(100000, 999999).ToString(); // 6 chữ số
            _httpContextAccessor.HttpContext.Session.SetString("OtpCode", otpCode);
            _httpContextAccessor.HttpContext.Session.SetString("OtpEmail", email);
            _httpContextAccessor.HttpContext.Session.SetString("OtpTimestamp", DateTime.UtcNow.ToString()); // Thời gian tạo OTP
            return otpCode;
        }

        public bool VerifyOtp(string email, string otp)
        {
            var storedOtp = _httpContextAccessor.HttpContext.Session.GetString("OtpCode");
            var storedEmail = _httpContextAccessor.HttpContext.Session.GetString("OtpEmail");
            var otpTimestampString = _httpContextAccessor.HttpContext.Session.GetString("OtpTimestamp");

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(storedEmail) || string.IsNullOrEmpty(otpTimestampString))
                return false;

            var otpTimestamp = DateTime.Parse(otpTimestampString);
            if ((DateTime.UtcNow - otpTimestamp).TotalMinutes > 10)
            {
                ClearOtpSession();
                return false;
            }

            if (storedOtp == otp && storedEmail == email)
            {
                ClearOtpSession();
                return true;
            }

            return false;
        }

        private void ClearOtpSession()
        {
            _httpContextAccessor.HttpContext.Session.Remove("OtpCode");
            _httpContextAccessor.HttpContext.Session.Remove("OtpEmail");
            _httpContextAccessor.HttpContext.Session.Remove("OtpTimestamp");
        }
    }
}
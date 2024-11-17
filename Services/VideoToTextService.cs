using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace BrainStormEra.Services
{
    public class VideoToTextService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;

        public VideoToTextService(string subscriptionKey, string region)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
        }

        public async Task<string> ConvertVideoToTextAsync(string videoFilePath)
        {
            if (!File.Exists(videoFilePath))
            {
                throw new FileNotFoundException("Video file not found", videoFilePath);
            }

            // Extract audio from video file (using FFmpeg or other tools)
            string audioFilePath = Path.ChangeExtension(videoFilePath, ".wav");
            ExtractAudioFromVideo(videoFilePath, audioFilePath);

            var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            using var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                return result.Text;
            }
            else
            {
                throw new Exception($"Speech recognition failed: {result.Reason}");
            }
        }

        private void ExtractAudioFromVideo(string videoFilePath, string audioFilePath)
        {
            var ffmpegPath = @"path\to\ffmpeg.exe"; // Path to FFmpeg executable
            var arguments = $"-i \"{videoFilePath}\" -vn -ar 16000 -ac 1 -ab 192k -f wav \"{audioFilePath}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}

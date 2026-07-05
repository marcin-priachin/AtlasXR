using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using UnityEngine.Networking;

namespace AtlasXR.Voice.TextToSpeech
{
    public sealed class OpenAITextToSpeechProvider : ITextToSpeechProvider
    {
        private const string SpeechUrl = "https://api.openai.com/v1/audio/speech";
        private readonly string apiKey;
        private readonly string model;
        private readonly string defaultVoice;
        private readonly IAtlasLogger logger;

        public OpenAITextToSpeechProvider(
            string apiKey,
            string model,
            string defaultVoice,
            IAtlasLogger logger)
        {
            this.apiKey = apiKey;
            this.model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini-tts" : model.Trim();
            this.defaultVoice = string.IsNullOrWhiteSpace(defaultVoice) ? "alloy" : defaultVoice.Trim();
            this.logger = logger;
        }

        public async Task<TextToSpeechResult> SynthesizeAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
            }

            var format = string.IsNullOrWhiteSpace(request.format) ? "wav" : request.format.Trim();
            var payload = BuildPayload(request, format);

            using (var webRequest = new UnityWebRequest(SpeechUrl, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(payload);
                webRequest.uploadHandler = new UploadHandlerRaw(body);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.SetRequestHeader("Content-Type", "application/json");

                await SendAsync(webRequest, cancellationToken);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(
                        $"OpenAI text-to-speech request failed: {webRequest.responseCode} {webRequest.error}");
                }

                logger?.Info("OpenAI text-to-speech synthesis completed.");
                return new TextToSpeechResult
                {
                    audioData = webRequest.downloadHandler.data,
                    format = format,
                    mimeType = GetMimeType(format)
                };
            }
        }

        private string BuildPayload(TextToSpeechRequest request, string format)
        {
            var voice = string.IsNullOrWhiteSpace(request.voice) ? defaultVoice : request.voice.Trim();
            var payload =
                "{" +
                $"\"model\":\"{JsonEscape(model)}\"," +
                $"\"voice\":\"{JsonEscape(voice)}\"," +
                $"\"input\":\"{JsonEscape(request.text.Trim())}\"," +
                $"\"response_format\":\"{JsonEscape(format)}\"";

            if (!string.IsNullOrWhiteSpace(request.instructions))
            {
                payload += $",\"instructions\":\"{JsonEscape(request.instructions.Trim())}\"";
            }

            return payload + "}";
        }

        private static async Task SendAsync(UnityWebRequest webRequest, CancellationToken cancellationToken)
        {
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    webRequest.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }
        }

        private static string GetMimeType(string format)
        {
            switch (format.ToLowerInvariant())
            {
                case "mp3":
                    return "audio/mpeg";
                case "opus":
                    return "audio/opus";
                case "aac":
                    return "audio/aac";
                case "flac":
                    return "audio/flac";
                case "pcm":
                    return "audio/pcm";
                case "wav":
                default:
                    return "audio/wav";
            }
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

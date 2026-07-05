using System;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace AtlasXR.Voice.SpeechToText
{
    public sealed class OpenAISpeechToTextProvider : ISpeechToTextProvider
    {
        private const string TranscriptionsUrl = "https://api.openai.com/v1/audio/transcriptions";
        private readonly string apiKey;
        private readonly string model;
        private readonly IAtlasLogger logger;

        public OpenAISpeechToTextProvider(string apiKey, string model, IAtlasLogger logger)
        {
            this.apiKey = apiKey;
            this.model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini-transcribe" : model.Trim();
            this.logger = logger;
        }

        public async Task<SpeechToTextResult> TranscribeAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
            }

            var form = new WWWForm();
            form.AddBinaryData(
                "file",
                request.audioData,
                string.IsNullOrWhiteSpace(request.fileName) ? "speech.wav" : request.fileName,
                string.IsNullOrWhiteSpace(request.mimeType) ? "audio/wav" : request.mimeType);
            form.AddField("model", model);
            form.AddField("response_format", "json");

            if (!string.IsNullOrWhiteSpace(request.prompt))
            {
                form.AddField("prompt", request.prompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(request.language))
            {
                form.AddField("language", request.language.Trim());
            }

            using (var webRequest = UnityWebRequest.Post(TranscriptionsUrl, form))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                await SendAsync(webRequest, cancellationToken);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(
                        $"OpenAI speech-to-text request failed: {webRequest.responseCode} {webRequest.error}");
                }

                var result = JsonUtility.FromJson<SpeechToTextResult>(webRequest.downloadHandler.text);
                if (result == null)
                {
                    throw new InvalidOperationException("OpenAI speech-to-text response did not include a transcript.");
                }

                result.text = result.text ?? string.Empty;
                logger?.Info("OpenAI speech-to-text transcription completed.");
                return result;
            }
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
    }
}

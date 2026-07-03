using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace AtlasXR.AI.Runtime
{
    public sealed class OpenAIAgentProvider : IAgentProvider
    {
        private const string ResponsesUrl = "https://api.openai.com/v1/responses";
        private readonly string apiKey;
        private readonly string model;
        private readonly IAtlasLogger logger;

        public OpenAIAgentProvider(string apiKey, string model, IAtlasLogger logger)
        {
            this.apiKey = apiKey;
            this.model = string.IsNullOrWhiteSpace(model) ? "gpt-5.5" : model.Trim();
            this.logger = logger;
        }

        public async Task<AgentResponse> GenerateResponseAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
            }

            var payload = BuildPayload(request);
            using (var webRequest = new UnityWebRequest(ResponsesUrl, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(payload);
                webRequest.uploadHandler = new UploadHandlerRaw(body);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.SetRequestHeader("Content-Type", "application/json");

                await SendAsync(webRequest, cancellationToken);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException($"OpenAI request failed: {webRequest.responseCode} {webRequest.error}");
                }

                var responseJson = webRequest.downloadHandler.text;
                var contentJson = OpenAIResponseParser.ExtractAssistantJson(responseJson);
                var response = JsonUtility.FromJson<AgentResponse>(contentJson);
                if (response == null)
                {
                    throw new InvalidOperationException("OpenAI response did not match the expected agent response format.");
                }

                response.toolCalls = response.toolCalls ?? new System.Collections.Generic.List<AgentToolCall>();
                logger?.Info($"OpenAI agent returned {response.toolCalls.Count} tool call(s).");
                return response;
            }
        }

        private string BuildPayload(AgentRequest request)
        {
            var prompt = MaintenanceAssistantPrompt.Build(request);
            return
                "{" +
                $"\"model\":\"{JsonEscape(model)}\"," +
                "\"input\":[" +
                "{\"role\":\"system\",\"content\":\"You are an AtlasXR maintenance assistant. Return only valid JSON matching the response schema.\"}," +
                $"{{\"role\":\"user\",\"content\":\"{JsonEscape(prompt)}\"}}" +
                "]," +
                "\"text\":{\"format\":{\"type\":\"json_schema\",\"name\":\"atlasxr_agent_response\",\"strict\":true,\"schema\":" +
                MaintenanceAssistantPrompt.ResponseSchemaJson +
                "}}" +
                "}";
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

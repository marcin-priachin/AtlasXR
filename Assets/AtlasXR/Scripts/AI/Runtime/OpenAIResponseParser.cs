using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtlasXR.AI.Runtime
{
    internal static class OpenAIResponseParser
    {
        public static string ExtractAssistantJson(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new InvalidOperationException("OpenAI response body was empty.");
            }

            var response = JsonUtility.FromJson<ResponseEnvelope>(responseJson);
            if (!string.IsNullOrWhiteSpace(response?.output_text))
            {
                return response.output_text;
            }

            if (response?.output != null)
            {
                foreach (var output in response.output)
                {
                    if (output?.content == null)
                    {
                        continue;
                    }

                    foreach (var content in output.content)
                    {
                        if (!string.IsNullOrWhiteSpace(content?.text))
                        {
                            return content.text;
                        }
                    }
                }
            }

            throw new InvalidOperationException("OpenAI response did not contain assistant JSON text.");
        }

        [Serializable]
        private sealed class ResponseEnvelope
        {
            public string output_text;
            public List<ResponseOutput> output;
        }

        [Serializable]
        private sealed class ResponseOutput
        {
            public List<ResponseContent> content;
        }

        [Serializable]
        private sealed class ResponseContent
        {
            public string text;
        }
    }
}

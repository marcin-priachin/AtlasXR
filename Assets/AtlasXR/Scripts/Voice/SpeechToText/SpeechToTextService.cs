using System;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using AtlasXR.Voice.Shared;
using UnityEngine;

namespace AtlasXR.Voice.SpeechToText
{
    public sealed class SpeechToTextService : ISpeechToTextService
    {
        private readonly ISpeechToTextProvider provider;
        private readonly ISpeechToTextProvider fallbackProvider;
        private readonly IAtlasLogger logger;

        public SpeechToTextService(
            ISpeechToTextProvider provider,
            ISpeechToTextProvider fallbackProvider,
            IAtlasLogger logger)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.fallbackProvider = fallbackProvider;
            this.logger = logger;
        }

        public async Task<SpeechToTextResult> TranscribeAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.audioData == null || request.audioData.Length == 0)
            {
                return new SpeechToTextResult { text = string.Empty };
            }

            try
            {
                return await provider.TranscribeAsync(request, cancellationToken);
            }
            catch (Exception exception) when (fallbackProvider != null)
            {
                logger?.Warning($"Speech-to-text provider failed, using mock provider. {exception.Message}");
                return await fallbackProvider.TranscribeAsync(request, cancellationToken);
            }
        }

        public Task<SpeechToTextResult> TranscribeAsync(
            AudioClip audioClip,
            string prompt,
            CancellationToken cancellationToken)
        {
            if (audioClip == null)
            {
                throw new ArgumentNullException(nameof(audioClip));
            }

            var request = new SpeechToTextRequest
            {
                audioData = WavAudioUtility.Encode(audioClip),
                fileName = "speech.wav",
                mimeType = "audio/wav",
                prompt = prompt
            };

            return TranscribeAsync(request, cancellationToken);
        }
    }
}

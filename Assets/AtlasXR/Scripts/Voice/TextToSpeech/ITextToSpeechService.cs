using System;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using AtlasXR.Voice.Shared;
using UnityEngine;

namespace AtlasXR.Voice.TextToSpeech
{
    public interface ITextToSpeechService
    {
        Task<TextToSpeechResult> SynthesizeAsync(TextToSpeechRequest request, CancellationToken cancellationToken);

        Task<AudioClip> SynthesizeClipAsync(TextToSpeechRequest request, CancellationToken cancellationToken);
    }

    public sealed class FallbackTextToSpeechService : ITextToSpeechService
    {
        private readonly ITextToSpeechProvider provider;
        private readonly ITextToSpeechProvider fallbackProvider;
        private readonly IAtlasLogger logger;

        public FallbackTextToSpeechService(
            ITextToSpeechProvider provider,
            ITextToSpeechProvider fallbackProvider,
            IAtlasLogger logger)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.fallbackProvider = fallbackProvider;
            this.logger = logger;
        }

        public async Task<TextToSpeechResult> SynthesizeAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.text))
            {
                throw new ArgumentException("Text-to-speech request text is required.", nameof(request));
            }

            try
            {
                return await provider.SynthesizeAsync(request, cancellationToken);
            }
            catch (Exception exception) when (fallbackProvider != null)
            {
                logger?.Warning($"Text-to-speech provider failed, using mock provider. {exception.Message}");
                return await fallbackProvider.SynthesizeAsync(request, cancellationToken);
            }
        }

        public async Task<AudioClip> SynthesizeClipAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken)
        {
            var result = await SynthesizeAsync(request, cancellationToken);
            if (result == null || result.audioData == null || result.audioData.Length == 0)
            {
                throw new InvalidOperationException("Text-to-speech provider returned no audio.");
            }

            if (!string.Equals(result.format, "wav", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only WAV text-to-speech output can be converted to an AudioClip.");
            }

            return WavAudioUtility.Decode(result.audioData, "OpenAI Speech");
        }
    }
}

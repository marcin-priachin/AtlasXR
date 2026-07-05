using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Voice.Shared;

namespace AtlasXR.Voice.TextToSpeech
{
    public sealed class MockTextToSpeechProvider : ITextToSpeechProvider
    {
        public Task<TextToSpeechResult> SynthesizeAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new TextToSpeechResult
            {
                audioData = WavAudioUtility.CreateTone(0.35f, 24000, 1, 880f, 0.35f),
                format = "wav",
                mimeType = "audio/wav"
            });
        }
    }
}

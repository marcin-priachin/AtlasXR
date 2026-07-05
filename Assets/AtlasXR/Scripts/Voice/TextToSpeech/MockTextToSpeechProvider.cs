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
                audioData = WavAudioUtility.CreateSilence(0.25f, 24000, 1),
                format = "wav",
                mimeType = "audio/wav"
            });
        }
    }
}

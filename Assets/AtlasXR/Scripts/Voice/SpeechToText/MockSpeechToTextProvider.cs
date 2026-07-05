using System.Threading;
using System.Threading.Tasks;

namespace AtlasXR.Voice.SpeechToText
{
    public sealed class MockSpeechToTextProvider : ISpeechToTextProvider
    {
        private readonly string transcript;

        public MockSpeechToTextProvider(string transcript = "How do I replace the filter?")
        {
            this.transcript = transcript;
        }

        public Task<SpeechToTextResult> TranscribeAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new SpeechToTextResult { text = transcript });
        }
    }
}

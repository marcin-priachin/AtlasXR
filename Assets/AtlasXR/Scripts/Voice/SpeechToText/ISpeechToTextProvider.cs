using System.Threading;
using System.Threading.Tasks;

namespace AtlasXR.Voice.SpeechToText
{
    public interface ISpeechToTextProvider
    {
        Task<SpeechToTextResult> TranscribeAsync(SpeechToTextRequest request, CancellationToken cancellationToken);
    }
}

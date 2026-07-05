using System.Threading;
using System.Threading.Tasks;

namespace AtlasXR.Voice.TextToSpeech
{
    public interface ITextToSpeechProvider
    {
        Task<TextToSpeechResult> SynthesizeAsync(TextToSpeechRequest request, CancellationToken cancellationToken);
    }
}

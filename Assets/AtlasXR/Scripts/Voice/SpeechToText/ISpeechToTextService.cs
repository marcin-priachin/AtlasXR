using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AtlasXR.Voice.SpeechToText
{
    public interface ISpeechToTextService
    {
        Task<SpeechToTextResult> TranscribeAsync(SpeechToTextRequest request, CancellationToken cancellationToken);

        Task<SpeechToTextResult> TranscribeAsync(
            AudioClip audioClip,
            string prompt,
            CancellationToken cancellationToken);
    }
}

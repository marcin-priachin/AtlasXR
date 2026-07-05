namespace AtlasXR.Voice.SpeechToText
{
    public sealed class SpeechToTextRequest
    {
        public byte[] audioData;

        public string fileName = "speech.wav";

        public string mimeType = "audio/wav";

        public string prompt;

        public string language;
    }
}

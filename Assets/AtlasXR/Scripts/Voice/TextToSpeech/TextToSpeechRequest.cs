namespace AtlasXR.Voice.TextToSpeech
{
    public sealed class TextToSpeechRequest
    {
        public string text;

        public string voice;

        public string format = "wav";

        public string instructions;
    }
}

using UnityEngine;

namespace AtlasXR.Core.Logging
{
    public sealed class UnityAtlasLogger : IAtlasLogger
    {
        private const string Prefix = "[AtlasXR] ";

        public void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public void Warning(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }
}

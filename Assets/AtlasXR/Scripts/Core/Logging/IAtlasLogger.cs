namespace AtlasXR.Core.Logging
{
    public interface IAtlasLogger
    {
        void Info(string message);

        void Warning(string message);

        void Error(string message);
    }
}

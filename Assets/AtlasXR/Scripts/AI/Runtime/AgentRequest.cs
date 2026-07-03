using System;

namespace AtlasXR.AI.Runtime
{
    [Serializable]
    public sealed class AgentRequest
    {
        public string userCommand;
        public AgentContext context;
    }
}

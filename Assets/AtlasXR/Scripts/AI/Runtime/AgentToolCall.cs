using System;

namespace AtlasXR.AI.Runtime
{
    [Serializable]
    public sealed class AgentToolCall
    {
        public string tool;
        public string componentId;
        public string instruction;
        public string stepId;
        public string reason;
    }
}

using System;
using System.Collections.Generic;

namespace AtlasXR.AI.Runtime
{
    [Serializable]
    public sealed class AgentResponse
    {
        public string assistantMessage;
        public List<AgentToolCall> toolCalls = new List<AgentToolCall>();
    }
}

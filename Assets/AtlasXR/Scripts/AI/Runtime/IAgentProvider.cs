using System.Threading;
using System.Threading.Tasks;

namespace AtlasXR.AI.Runtime
{
    public interface IAgentProvider
    {
        Task<AgentResponse> GenerateResponseAsync(AgentRequest request, CancellationToken cancellationToken);
    }
}

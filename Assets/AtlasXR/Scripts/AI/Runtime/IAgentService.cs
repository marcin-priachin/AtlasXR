using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Procedures.Data;

namespace AtlasXR.AI.Runtime
{
    public interface IAgentService
    {
        Task<AgentResponse> HandleCommandAsync(
            string userCommand,
            ProcedureDefinition procedure,
            int currentStepIndex,
            CancellationToken cancellationToken);
    }
}

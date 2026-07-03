using System;
using System.Threading;
using System.Threading.Tasks;
using AtlasXR.Core.Logging;
using AtlasXR.Procedures.Data;

namespace AtlasXR.AI.Runtime
{
    public sealed class AgentService : IAgentService
    {
        private readonly IAgentProvider provider;
        private readonly IAgentProvider fallbackProvider;
        private readonly IAtlasLogger logger;

        public AgentService(IAgentProvider provider, IAgentProvider fallbackProvider, IAtlasLogger logger)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.fallbackProvider = fallbackProvider;
            this.logger = logger;
        }

        public async Task<AgentResponse> HandleCommandAsync(
            string userCommand,
            ProcedureDefinition procedure,
            int currentStepIndex,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userCommand))
            {
                return new AgentResponse { assistantMessage = "Enter a maintenance command." };
            }

            var request = new AgentRequest
            {
                userCommand = userCommand.Trim(),
                context = AgentContext.FromProcedure(procedure, currentStepIndex)
            };

            try
            {
                return await provider.GenerateResponseAsync(request, cancellationToken);
            }
            catch (Exception exception) when (fallbackProvider != null)
            {
                logger?.Warning($"Agent provider failed, using mock provider. {exception.Message}");
                return await fallbackProvider.GenerateResponseAsync(request, cancellationToken);
            }
        }
    }
}

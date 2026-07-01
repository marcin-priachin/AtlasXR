using System;
using System.Collections.Generic;

namespace AtlasXR.Core.StateMachine
{
    public sealed class StateMachine<TStateKey>
    {
        private readonly Dictionary<TStateKey, IState> states = new Dictionary<TStateKey, IState>();

        public TStateKey CurrentStateKey { get; private set; }

        public IState CurrentState { get; private set; }

        public bool HasState => CurrentState != null;

        public void RegisterState(TStateKey key, IState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            states[key] = state;
        }

        public void ChangeState(TStateKey key)
        {
            if (!states.TryGetValue(key, out var nextState))
            {
                throw new InvalidOperationException($"State is not registered: {key}");
            }

            CurrentState?.Exit();
            CurrentStateKey = key;
            CurrentState = nextState;
            CurrentState.Enter();
        }
    }
}

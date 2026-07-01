using AtlasXR.Core.StateMachine;

namespace AtlasXR.App.States
{
    public abstract class AppState : IState
    {
        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }
    }
}

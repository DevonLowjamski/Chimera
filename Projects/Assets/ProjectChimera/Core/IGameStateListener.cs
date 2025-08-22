

namespace ProjectChimera.Core
{
    public interface IGameStateListener
    {
        void OnGameStateChanged(GameState newGameState);
    }
}

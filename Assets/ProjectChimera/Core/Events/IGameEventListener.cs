namespace ProjectChimera.Core.Events
{
    public interface IGameEventListener<T>
    {
        void OnEventRaised(T item);
    }
}

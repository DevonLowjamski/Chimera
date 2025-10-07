using UnityEngine;
using ProjectChimera.Core.Events;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Scene
{
    [CreateAssetMenu(fileName = "SceneLoadStartedEvent", menuName = "ProjectChimera/Scene Events/Scene Load Started")]
    public class SceneLoadStartedEventSO : StringGameEventSO
    {
    }

    [CreateAssetMenu(fileName = "SceneLoadCompletedEvent", menuName = "ProjectChimera/Scene Events/Scene Load Completed")]
    public class SceneLoadCompletedEventSO : StringGameEventSO
    {
    }

    [CreateAssetMenu(fileName = "SceneUnloadStartedEvent", menuName = "ProjectChimera/Scene Events/Scene Unload Started")]
    public class SceneUnloadStartedEventSO : StringGameEventSO
    {
    }

    [CreateAssetMenu(fileName = "SceneUnloadCompletedEvent", menuName = "ProjectChimera/Scene Events/Scene Unload Completed")]
    public class SceneUnloadCompletedEventSO : StringGameEventSO
    {
    }

    [CreateAssetMenu(fileName = "SceneTransitionCompletedEvent", menuName = "ProjectChimera/Scene Events/Scene Transition Completed")]
    public class SceneTransitionCompletedEventSO : StringGameEventSO
    {
    }

    [System.Serializable]
    public class SceneTransitionData
    {
        public string fromScene;
        public string toScene;
        
        public SceneTransitionData(string from, string to)
        {
            fromScene = from;
            toScene = to;
        }
    }

    [CreateAssetMenu(fileName = "SceneTransitionStartedEvent", menuName = "ProjectChimera/Scene Events/Scene Transition Started")]
    public class SceneTransitionStartedEventSO : GameEventSO<SceneTransitionData>
    {
    }

    [System.Serializable]
    public class SceneLoadProgressData
    {
        public string sceneName;
        public float progress;
        
        public SceneLoadProgressData(string name, float progressValue)
        {
            sceneName = name;
            progress = progressValue;
        }
    }

    [CreateAssetMenu(fileName = "SceneLoadProgressEvent", menuName = "ProjectChimera/Scene Events/Scene Load Progress")]
    public class SceneLoadProgressEventSO : GameEventSO<SceneLoadProgressData>
    {
    }
}
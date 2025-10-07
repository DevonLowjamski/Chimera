using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Core
{
    // Minimal stubs used by early-phase UI to avoid heavy dependencies
    public enum UIAnnouncementPriority { Low, Normal, High }
    // Remove duplicate UIStatus; source of truth is UI/Components/UIComponents.cs
    /* public enum UIStatus
    {
        None,
        Info,
        Warning,
        Error,
        Success
    } */

    public class LeaderboardEntryUI : MonoBehaviour {}
    public class ForumPostUI : MonoBehaviour {}
    public class CommunityContentUI : MonoBehaviour {}
}
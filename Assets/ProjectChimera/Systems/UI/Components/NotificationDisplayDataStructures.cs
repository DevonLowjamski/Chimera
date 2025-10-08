// REFACTORED: Notification Display Data Structures
// Extracted from NotificationDisplay for better separation of concerns

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectChimera.Systems.UI.Components
{
    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Notification data
    /// </summary>
    [Serializable]
    public struct NotificationData
    {
        public string Id;
        public string Message;
        public NotificationType Type;
        public float Duration;
        public float Timestamp;
        public int Priority;
    }

    /// <summary>
    /// UI notification element
    /// </summary>
    public class NotificationElement
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public Image BackgroundImage;
        public TextMeshProUGUI TextComponent;
        public CanvasGroup CanvasGroup;
        public NotificationData Data;
    }
}


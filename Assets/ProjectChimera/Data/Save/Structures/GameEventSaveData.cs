using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    public class GameEventSaveData
    {
        public string EventId;
        public string EventType;
        public DateTime Timestamp;
        public Dictionary<string, object> EventData;
    }
}

using System;

namespace ProjectChimera.Systems.Environment
{
    [System.Serializable]
    public class LightingAlarm
    {
        public string AlarmId;
        public LightingAlarmType Type;
        public string Message;
        public DateTime Timestamp;
    }

    public enum LightingAlarmType
    {
        BulbFailure,
        PowerSurge,
        Overheating
    }
}

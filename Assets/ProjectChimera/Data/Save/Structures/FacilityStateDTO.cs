using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save.Structures
{
    public class FacilityStateDTO
    {
        public System.DateTime SaveTimestamp;
        public string FacilityName;
        public string FacilityType;
        public int FacilityLevel;
        public string FacilityId;
        public UnityEngine.Vector3 Position;
        public UnityEngine.Vector3 Size;
        public bool IsOperational;
        public int RoomCount;
        public int EquipmentCount;
        public float PowerConsumption;
        public System.DateTime LastUpdate;
    }
}

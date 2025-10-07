using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    public class ProgressionSaveData
    {
        public int PlayerLevel;
        public Dictionary<string, SkillSaveData> Skills;
    }
}

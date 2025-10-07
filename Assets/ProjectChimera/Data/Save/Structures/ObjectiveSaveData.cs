using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    public class ObjectiveSaveData
    {
        public List<QuestSaveData> ActiveQuests;
        public List<AchievementSaveData> Achievements;
    }
}

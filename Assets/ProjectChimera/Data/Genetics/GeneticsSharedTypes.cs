using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Shared types for genetics system - single source of truth for all genetics data structures
    /// This eliminates circular dependencies and namespace conflicts
    /// </summary>
    
    #region Core Genetics Types
    
    [Serializable]
    public class GeneticsBreedingResult
    {
        public bool Success = true;
        public string OffspringID = "";
        public string Parent1ID = "";
        public string Parent2ID = "";
        public Dictionary<string, float> ExpressedTraits = new Dictionary<string, float>();
        public float BreedingAccuracy = 0f;
        public DateTime BreedingDate = DateTime.Now;
        public string Notes = "";
    }
    
    [Serializable]
    public class GeneticsSensoryResponse
    {
        public bool IsCorrect = false;
        public string TerpeneIdentified = "";
        public string CorrectAnswer = "";
        public float ResponseTime = 0f;
        public float Confidence = 0.5f;
        public DateTime ResponseTime_DateTime = DateTime.Now;
    }
    
    [Serializable]
    public class GeneticsCompetitionEntry
    {
        public string EntryID = "";
        public string ParticipantID = "";
        public string CompetitionID = "";
        public bool IsActive = true;
        public float Score = 0f;
        public DateTime EntryTime = DateTime.Now;
    }
    
    [Serializable]
    public class GeneticsBreedingChallenge
    {
        public string ChallengeID = "";
        public string ChallengeName = "";
        public string Description = "";
        public List<string> RequiredTraits = new List<string>();
        public List<string> AvailableParents = new List<string>();
        public int MaxGenerations = 5;
        public float TimeLimit = 300f;
        public bool IsActive = true;
    }
    
    [Serializable]
    public class GeneticsTournament
    {
        public string TournamentID = "";
        public string TournamentName = "";
        public string Description = "";
        public int MaxParticipants = 100;
        public int CurrentParticipants = 0;
        public bool IsActive = true;
        public DateTime StartTime = DateTime.Now;
        public DateTime EndTime = DateTime.Now.AddDays(1);
        public List<string> ParticipantIDs = new List<string>();
    }
    
    #endregion
    
    #region Simple Enums
    
    public enum GeneticsDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }
    
    public enum GeneticsGameMode
    {
        Breeding,
        Sensory,
        Competition,
        Research
    }
    
    public enum GeneticsCompetitionType
    {
        Individual,
        Team,
        Tournament,
        Challenge
    }
    
    #endregion
}
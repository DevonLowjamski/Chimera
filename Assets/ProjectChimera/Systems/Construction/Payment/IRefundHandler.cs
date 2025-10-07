using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Interface for refund processing and resource reservation management
    /// </summary>
    public interface IRefundHandler
    {
        bool EnableRefunds { get; set; }
        float RefundPercentage { get; set; }
        bool EnableResourceReservation { get; set; }
        float ReservationDuration { get; set; }
        bool AutoReleaseReservations { get; set; }
        int MaxSimultaneousReservations { get; set; }
        
        Dictionary<string, ResourceReservation> ActiveReservations { get; }
        Dictionary<Vector3Int, List<string>> PositionReservations { get; }
        
        RefundResult ProcessRefund(Vector3Int gridPosition, string reason = "");
        List<ResourceCost> CalculateResourceRefund(List<ResourceCost> originalCosts);
        
        ResourceReservationResult CreateReservation(GridPlaceable placeable, Vector3Int gridPosition);
        bool ReleaseReservation(string reservationId);
        void ProcessExpiredReservations();
        
        bool ReserveResources(List<ResourceCost> resources);
        void ReleaseReservedResources(List<ResourceCost> resources);
        void RefundResources(List<ResourceCost> resources);
        
        bool ReserveResourceFromInventory(string resourceId, int quantity);
        void ReleaseReservedResourceFromInventory(string resourceId, int quantity);
        void AddResourceToInventory(string resourceId, int quantity);
        
        string GetPlayerID();
        void Tick(float deltaTime);
        
        // Events
        Action<string, ResourceReservation> OnResourceReserved { get; set; }
        Action<string> OnReservationReleased { get; set; }
        
        void Initialize(bool enableRefunds, float refundPercentage, bool enableResourceReservation,
                       float reservationDuration, bool autoReleaseReservations, int maxSimultaneousReservations);
        void Shutdown();
    }

    public struct ResourceReservation
    {
        public string ReservationId;
        public Vector3Int Position;
        public List<ResourceCost> ReservedResources;
        public float ReservationTime;
        public float ExpiryTime;
        public bool IsActive;
        public string PlayerId;
    }

    public struct ResourceReservationResult
    {
        public bool Success;
        public string ReservationId;
        public string ErrorMessage;
        public float ExpiryTime;
    }

    public struct RefundResult
    {
        public bool Success;
        public float RefundAmount;
        public List<ResourceCost> RefundedResources;
        public string ErrorMessage;
    }
}

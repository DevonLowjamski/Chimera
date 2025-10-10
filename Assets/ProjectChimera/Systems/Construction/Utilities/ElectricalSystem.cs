using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.Utilities
{
    /// <summary>
    /// Electrical utility system - manages power distribution across the facility.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Electricity powers everything - plan your capacity wisely!"
    ///
    /// **Player Experience**:
    /// - Place electrical panels (breaker boxes) with capacity (e.g., 100A, 200A)
    /// - Route wires from panels to equipment
    /// - See total load vs. capacity in real-time
    /// - Equipment won't operate without power connection
    /// - Circuit breakers trip if overloaded
    ///
    /// **Strategic Depth**:
    /// - Start small (1x 100A panel)
    /// - Upgrade as facility grows (multiple panels, sub-panels)
    /// - Balance cost vs. future expansion
    /// - Penalties for overloading (equipment damage, fire risk)
    ///
    /// **Construction Integration**:
    /// - Electricity required before placing equipment
    /// - Wire routing with pathfinding (A* algorithm)
    /// - Cost per foot of wire + connection labor
    /// - Visual wire overlay in construction mode
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Panel 1: 75A / 200A (37% load)" → simple!
    /// Behind scenes: Graph-based power network, load calculations, routing algorithms.
    /// </summary>
    public class ElectricalSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _wireC​ostPerFoot = 2.5f;  // $2.50 per foot of wire
        [SerializeField] private float _panelInstallationCost = 500f;
        [SerializeField] private float _connectionLaborCost = 50f;

        [Header("Safety")]
        [SerializeField] private float _overloadThreshold = 0.95f;  // 95% capacity triggers warning
        [SerializeField] private float _criticalOverload = 1.05f;   // 105% trips breaker

        // Electrical network graph
        private Dictionary<string, ElectricalPanel> _panels = new Dictionary<string, ElectricalPanel>();
        private Dictionary<string, ElectricalConnection> _connections = new Dictionary<string, ElectricalConnection>();
        private Dictionary<string, PoweredDevice> _devices = new Dictionary<string, PoweredDevice>();

        // Events
        public event Action<string> OnPanelAdded;                    // panelId
        public event Action<string, float, float> OnLoadChanged;     // panelId, currentLoad, capacity
        public event Action<string, string> OnCircuitBreakerTripped; // panelId, reason
        public event Action<string> OnDevicePowered;                 // deviceId
        public event Action<string> OnDeviceUnpowered;               // deviceId

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<ElectricalSystem>(this);

            ChimeraLogger.Log("ELECTRICAL",
                "Electrical system initialized - ready to power your facility!", this);
        }

        #region Panel Management

        /// <summary>
        /// Adds an electrical panel (breaker box) at a location.
        /// GAMEPLAY: Player places panel in construction mode, pays installation cost.
        /// </summary>
        public bool AddPanel(string panelId, Vector3 location, float capacityAmps, float voltage = 240f)
        {
            if (_panels.ContainsKey(panelId))
            {
                ChimeraLogger.LogWarning("ELECTRICAL",
                    $"Panel {panelId} already exists", this);
                return false;
            }

            var panel = new ElectricalPanel
            {
                PanelId = panelId,
                Location = location,
                CapacityAmps = capacityAmps,
                Voltage = voltage,
                CurrentLoadAmps = 0f,
                IsActive = true,
                ConnectedDevices = new List<string>()
            };

            _panels[panelId] = panel;
            OnPanelAdded?.Invoke(panelId);

            ChimeraLogger.Log("ELECTRICAL",
                $"Panel {panelId} installed: {capacityAmps}A capacity at {location}", this);

            return true;
        }

        /// <summary>
        /// Removes an electrical panel.
        /// GAMEPLAY: Player demolishes panel, gets partial refund.
        /// </summary>
        public bool RemovePanel(string panelId)
        {
            if (!_panels.ContainsKey(panelId))
                return false;

            var panel = _panels[panelId];

            // Disconnect all devices first
            foreach (var deviceId in panel.ConnectedDevices.ToList())
            {
                DisconnectDevice(deviceId);
            }

            _panels.Remove(panelId);

            ChimeraLogger.Log("ELECTRICAL",
                $"Panel {panelId} removed", this);

            return true;
        }

        /// <summary>
        /// Gets panel info for UI display.
        /// GAMEPLAY: Shows in panel inspection UI.
        /// </summary>
        public ElectricalPanel GetPanel(string panelId)
        {
            return _panels.TryGetValue(panelId, out var panel) ? panel : default;
        }

        /// <summary>
        /// Gets all panels in facility.
        /// </summary>
        public List<ElectricalPanel> GetAllPanels()
        {
            return _panels.Values.ToList();
        }

        #endregion

        #region Device Connection

        /// <summary>
        /// Connects a device to a panel with automatic wire routing.
        /// GAMEPLAY: Player selects device → selects panel → wire routes automatically.
        /// </summary>
        public bool ConnectDevice(string deviceId, string panelId, Vector3 deviceLocation, float deviceLoadAmps)
        {
            if (!_panels.ContainsKey(panelId))
            {
                ChimeraLogger.LogWarning("ELECTRICAL",
                    $"Cannot connect device {deviceId}: Panel {panelId} not found", this);
                return false;
            }

            if (_devices.ContainsKey(deviceId))
            {
                ChimeraLogger.LogWarning("ELECTRICAL",
                    $"Device {deviceId} already connected", this);
                return false;
            }

            var panel = _panels[panelId];

            // Check capacity
            if (panel.CurrentLoadAmps + deviceLoadAmps > panel.CapacityAmps * _criticalOverload)
            {
                ChimeraLogger.LogWarning("ELECTRICAL",
                    $"Cannot connect device {deviceId}: Panel {panelId} would be critically overloaded", this);
                return false;
            }

            // Calculate wire path
            var wirePath = CalculateWirePath(panel.Location, deviceLocation);
            float wireLength = CalculateWireLength(wirePath);

            // Create connection
            var connection = new ElectricalConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                PanelId = panelId,
                WirePath = wirePath,
                WireLength = wireLength,
                InstallationCost = (wireLength * _wireCostPerFoot) + _connectionLaborCost,
                IsActive = true
            };

            _connections[connection.ConnectionId] = connection;

            // Register device
            var device = new PoweredDevice
            {
                DeviceId = deviceId,
                DeviceName = deviceId, // TODO: Get actual device name
                Location = deviceLocation,
                LoadAmps = deviceLoadAmps,
                ConnectionId = connection.ConnectionId,
                IsPowered = true
            };

            _devices[deviceId] = device;

            // Update panel load
            panel.CurrentLoadAmps += deviceLoadAmps;
            panel.ConnectedDevices.Add(deviceId);

            OnDevicePowered?.Invoke(deviceId);
            OnLoadChanged?.Invoke(panelId, panel.CurrentLoadAmps, panel.CapacityAmps);

            // Check for overload warning
            if (panel.CurrentLoadAmps > panel.CapacityAmps * _overloadThreshold)
            {
                ChimeraLogger.LogWarning("ELECTRICAL",
                    $"⚠️ Panel {panelId} approaching capacity: {panel.CurrentLoadAmps:F1}A / {panel.CapacityAmps}A", this);
            }

            ChimeraLogger.Log("ELECTRICAL",
                $"Device {deviceId} connected to {panelId}: {deviceLoadAmps}A load, {wireLength:F1}ft wire", this);

            return true;
        }

        /// <summary>
        /// Disconnects a device from power.
        /// GAMEPLAY: Player demolishes equipment or disconnects manually.
        /// </summary>
        public bool DisconnectDevice(string deviceId)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
                return false;

            // Find connection
            if (!_connections.TryGetValue(device.ConnectionId, out var connection))
                return false;

            // Update panel load
            if (_panels.TryGetValue(connection.PanelId, out var panel))
            {
                panel.CurrentLoadAmps -= device.LoadAmps;
                panel.ConnectedDevices.Remove(deviceId);
                OnLoadChanged?.Invoke(connection.PanelId, panel.CurrentLoadAmps, panel.CapacityAmps);
            }

            // Remove device and connection
            _devices.Remove(deviceId);
            _connections.Remove(device.ConnectionId);

            OnDeviceUnpowered?.Invoke(deviceId);

            ChimeraLogger.Log("ELECTRICAL",
                $"Device {deviceId} disconnected", this);

            return true;
        }

        /// <summary>
        /// Checks if device is powered.
        /// GAMEPLAY: Equipment checks this before operating.
        /// </summary>
        public bool IsDevicePowered(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) && device.IsPowered;
        }

        /// <summary>
        /// Gets device power info.
        /// </summary>
        public PoweredDevice GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) ? device : default;
        }

        #endregion

        #region Wire Routing

        /// <summary>
        /// Calculates wire path from panel to device using Manhattan distance heuristic.
        /// Simplified pathfinding for Phase 1 - can be enhanced to A* in Phase 2.
        /// </summary>
        private List<Vector3> CalculateWirePath(Vector3 from, Vector3 to)
        {
            // Simple direct path for Phase 1
            // TODO Phase 2: Implement A* with obstacle avoidance
            var path = new List<Vector3>();

            // Manhattan-style routing (right angles only, professional install style)
            path.Add(from);

            // Route along walls/ceiling (simplified)
            Vector3 corner1 = new Vector3(from.x, from.y, to.z);
            path.Add(corner1);

            path.Add(to);

            return path;
        }

        /// <summary>
        /// Calculates total wire length from path.
        /// </summary>
        private float CalculateWireLength(List<Vector3> path)
        {
            float totalLength = 0f;

            for (int i = 0; i < path.Count - 1; i++)
            {
                totalLength += Vector3.Distance(path[i], path[i + 1]);
            }

            return totalLength;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets panel utilization percentage.
        /// GAMEPLAY: Displayed in UI as "75% capacity"
        /// </summary>
        public float GetPanelUtilization(string panelId)
        {
            if (!_panels.TryGetValue(panelId, out var panel))
                return 0f;

            return (panel.CurrentLoadAmps / panel.CapacityAmps) * 100f;
        }

        /// <summary>
        /// Gets total facility power capacity.
        /// </summary>
        public float GetTotalCapacity()
        {
            return _panels.Values.Sum(p => p.CapacityAmps);
        }

        /// <summary>
        /// Gets total facility power load.
        /// </summary>
        public float GetTotalLoad()
        {
            return _panels.Values.Sum(p => p.CurrentLoadAmps);
        }

        /// <summary>
        /// Gets electrical system statistics for UI.
        /// </summary>
        public ElectricalStats GetStatistics()
        {
            return new ElectricalStats
            {
                TotalPanels = _panels.Count,
                TotalDevices = _devices.Count,
                TotalCapacityAmps = GetTotalCapacity(),
                TotalLoadAmps = GetTotalLoad(),
                TotalWireFeet = _connections.Values.Sum(c => c.WireLength),
                AverageUtilization = _panels.Values.Average(p => (p.CurrentLoadAmps / p.CapacityAmps) * 100f)
            };
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public struct ElectricalPanel
    {
        public string PanelId;
        public Vector3 Location;
        public float CapacityAmps;
        public float Voltage;
        public float CurrentLoadAmps;
        public bool IsActive;
        public List<string> ConnectedDevices;
    }

    [Serializable]
    public struct ElectricalConnection
    {
        public string ConnectionId;
        public string DeviceId;
        public string PanelId;
        public List<Vector3> WirePath;
        public float WireLength;
        public float InstallationCost;
        public bool IsActive;
    }

    [Serializable]
    public struct PoweredDevice
    {
        public string DeviceId;
        public string DeviceName;
        public Vector3 Location;
        public float LoadAmps;
        public string ConnectionId;
        public bool IsPowered;
    }

    [Serializable]
    public struct ElectricalStats
    {
        public int TotalPanels;
        public int TotalDevices;
        public float TotalCapacityAmps;
        public float TotalLoadAmps;
        public float TotalWireFeet;
        public float AverageUtilization;
    }

    #endregion
}

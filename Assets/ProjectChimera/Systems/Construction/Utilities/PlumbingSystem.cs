using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction.Utilities
{
    /// <summary>
    /// Plumbing/water utility system - manages water distribution and drainage.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Water is life - route it wisely to keep plants thriving!"
    ///
    /// **Player Experience**:
    /// - Place water sources (main line connections, reservoirs)
    /// - Route pipes to irrigation systems and equipment
    /// - Monitor flow rate (GPM) and pressure (PSI)
    /// - Drainage system prevents flooding
    /// - Fertigation integration (nutrients through irrigation)
    ///
    /// **Strategic Depth**:
    /// - Start with single water source
    /// - Upgrade to multiple zones for large facilities
    /// - Pressure management (pumps, regulators)
    /// - Water filtration/treatment systems
    /// - Cost-effective pipe sizing (bigger pipes = more capacity, higher cost)
    ///
    /// **Construction Integration**:
    /// - Irrigation systems require water connection
    /// - Pipe routing with diameter selection
    /// - Cost per foot (varies by diameter)
    /// - Visual pipe overlay in construction mode
    /// - Drainage integration prevents water accumulation
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Zone 1: 15 GPM / 25 GPM capacity, 45 PSI" → simple!
    /// Behind scenes: Hydraulic calculations, flow distribution, pressure drop modeling.
    /// </summary>
    public class PlumbingSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _smallPipeCostPerFoot = 3f;   // 0.5" pipe
        [SerializeField] private float _mediumPipeCostPerFoot = 5f;  // 1" pipe
        [SerializeField] private float _largePipeCostPerFoot = 8f;   // 2" pipe
        [SerializeField] private float _sourceInstallationCost = 750f;
        [SerializeField] private float _connectionLaborCost = 75f;

        [Header("Hydraulics")]
        [SerializeField] private float _minimumPressurePSI = 30f;    // Minimum pressure for operation
        [SerializeField] private float _nominalPressurePSI = 60f;    // Normal operating pressure
        [SerializeField] private float _pressureDropPerFoot = 0.1f;  // Simplified pressure drop

        // Plumbing network graph
        private Dictionary<string, WaterSource> _sources = new Dictionary<string, WaterSource>();
        private Dictionary<string, PlumbingConnection> _connections = new Dictionary<string, PlumbingConnection>();
        private Dictionary<string, WaterDevice> _devices = new Dictionary<string, WaterDevice>();

        // Events
        public event Action<string> OnSourceAdded;                      // sourceId
        public event Action<string, float, float> OnFlowChanged;        // sourceId, currentFlow, capacity
        public event Action<string> OnLowPressureWarning;               // deviceId
        public event Action<string> OnDeviceConnected;                  // deviceId
        public event Action<string> OnDeviceDisconnected;               // deviceId

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<PlumbingSystem>(this);

            ChimeraLogger.Log("PLUMBING",
                "Plumbing system initialized - ready to distribute water!", this);
        }

        #region Water Source Management

        /// <summary>
        /// Adds a water source (main line connection, reservoir, well).
        /// GAMEPLAY: Player places water source in construction mode.
        /// </summary>
        public bool AddSource(string sourceId, Vector3 location, float capacityGPM, float pressurePSI = 60f)
        {
            if (_sources.ContainsKey(sourceId))
            {
                ChimeraLogger.LogWarning("PLUMBING",
                    $"Source {sourceId} already exists", this);
                return false;
            }

            var source = new WaterSource
            {
                SourceId = sourceId,
                Location = location,
                CapacityGPM = capacityGPM,
                PressurePSI = pressurePSI,
                CurrentFlowGPM = 0f,
                IsActive = true,
                ConnectedDevices = new List<string>()
            };

            _sources[sourceId] = source;
            OnSourceAdded?.Invoke(sourceId);

            ChimeraLogger.Log("PLUMBING",
                $"Water source {sourceId} installed: {capacityGPM} GPM capacity, {pressurePSI} PSI at {location}", this);

            return true;
        }

        /// <summary>
        /// Removes a water source.
        /// GAMEPLAY: Player demolishes water source.
        /// </summary>
        public bool RemoveSource(string sourceId)
        {
            if (!_sources.ContainsKey(sourceId))
                return false;

            var source = _sources[sourceId];

            // Disconnect all devices first
            foreach (var deviceId in source.ConnectedDevices.ToList())
            {
                DisconnectDevice(deviceId);
            }

            _sources.Remove(sourceId);

            ChimeraLogger.Log("PLUMBING",
                $"Water source {sourceId} removed", this);

            return true;
        }

        /// <summary>
        /// Gets water source info for UI display.
        /// </summary>
        public WaterSource GetSource(string sourceId)
        {
            return _sources.TryGetValue(sourceId, out var source) ? source : default;
        }

        /// <summary>
        /// Gets all water sources.
        /// </summary>
        public List<WaterSource> GetAllSources()
        {
            return _sources.Values.ToList();
        }

        #endregion

        #region Device Connection

        /// <summary>
        /// Connects a water device (irrigation system, humidifier) to a source.
        /// GAMEPLAY: Player selects device → selects source → pipe routes automatically.
        /// </summary>
        public bool ConnectDevice(string deviceId, string sourceId, Vector3 deviceLocation,
            float deviceFlowGPM, PipeDiameter pipeDiameter)
        {
            if (!_sources.ContainsKey(sourceId))
            {
                ChimeraLogger.LogWarning("PLUMBING",
                    $"Cannot connect device {deviceId}: Source {sourceId} not found", this);
                return false;
            }

            if (_devices.ContainsKey(deviceId))
            {
                ChimeraLogger.LogWarning("PLUMBING",
                    $"Device {deviceId} already connected", this);
                return false;
            }

            var source = _sources[sourceId];

            // Check capacity
            if (source.CurrentFlowGPM + deviceFlowGPM > source.CapacityGPM)
            {
                ChimeraLogger.LogWarning("PLUMBING",
                    $"Cannot connect device {deviceId}: Source {sourceId} capacity exceeded", this);
                return false;
            }

            // Calculate pipe path
            var pipePath = CalculatePipePath(source.Location, deviceLocation);
            float pipeLength = CalculatePipeLength(pipePath);
            float pressureAtDevice = CalculatePressureAtDevice(source.PressurePSI, pipeLength);

            // Check pressure
            if (pressureAtDevice < _minimumPressurePSI)
            {
                ChimeraLogger.LogWarning("PLUMBING",
                    $"⚠️ Device {deviceId} will have low pressure ({pressureAtDevice:F1} PSI < {_minimumPressurePSI} PSI minimum)", this);
                OnLowPressureWarning?.Invoke(deviceId);
            }

            // Create connection
            var connection = new PlumbingConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                SourceId = sourceId,
                PipePath = pipePath,
                PipeLength = pipeLength,
                PipeDiameter = pipeDiameter,
                InstallationCost = CalculatePipeCost(pipeLength, pipeDiameter) + _connectionLaborCost,
                IsActive = true
            };

            _connections[connection.ConnectionId] = connection;

            // Register device
            var device = new WaterDevice
            {
                DeviceId = deviceId,
                DeviceName = deviceId, // TODO: Get actual device name
                Location = deviceLocation,
                FlowGPM = deviceFlowGPM,
                PressurePSI = pressureAtDevice,
                ConnectionId = connection.ConnectionId,
                IsConnected = true
            };

            _devices[deviceId] = device;

            // Update source flow
            source.CurrentFlowGPM += deviceFlowGPM;
            source.ConnectedDevices.Add(deviceId);

            OnDeviceConnected?.Invoke(deviceId);
            OnFlowChanged?.Invoke(sourceId, source.CurrentFlowGPM, source.CapacityGPM);

            ChimeraLogger.Log("PLUMBING",
                $"Device {deviceId} connected to {sourceId}: {deviceFlowGPM} GPM, {pipeLength:F1}ft {pipeDiameter} pipe, {pressureAtDevice:F1} PSI", this);

            return true;
        }

        /// <summary>
        /// Disconnects a device from water.
        /// GAMEPLAY: Player demolishes equipment or disconnects manually.
        /// </summary>
        public bool DisconnectDevice(string deviceId)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
                return false;

            // Find connection
            if (!_connections.TryGetValue(device.ConnectionId, out var connection))
                return false;

            // Update source flow
            if (_sources.TryGetValue(connection.SourceId, out var source))
            {
                source.CurrentFlowGPM -= device.FlowGPM;
                source.ConnectedDevices.Remove(deviceId);
                OnFlowChanged?.Invoke(connection.SourceId, source.CurrentFlowGPM, source.CapacityGPM);
            }

            // Remove device and connection
            _devices.Remove(deviceId);
            _connections.Remove(device.ConnectionId);

            OnDeviceDisconnected?.Invoke(deviceId);

            ChimeraLogger.Log("PLUMBING",
                $"Device {deviceId} disconnected", this);

            return true;
        }

        /// <summary>
        /// Checks if device has adequate water supply.
        /// GAMEPLAY: Irrigation systems check this before operating.
        /// </summary>
        public bool IsDeviceConnected(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) && device.IsConnected;
        }

        /// <summary>
        /// Gets device water info.
        /// </summary>
        public WaterDevice GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) ? device : default;
        }

        #endregion

        #region Pipe Routing & Hydraulics

        /// <summary>
        /// Calculates pipe path from source to device.
        /// Simplified routing for Phase 1 - can be enhanced to A* in Phase 2.
        /// </summary>
        private List<Vector3> CalculatePipePath(Vector3 from, Vector3 to)
        {
            // Simple direct path for Phase 1
            // TODO Phase 2: Implement A* with obstacle avoidance
            var path = new List<Vector3>();

            // Manhattan-style routing (right angles only, professional install style)
            path.Add(from);

            // Route along floor/walls (simplified)
            Vector3 corner1 = new Vector3(from.x, from.y, to.z);
            path.Add(corner1);

            path.Add(to);

            return path;
        }

        /// <summary>
        /// Calculates total pipe length from path.
        /// </summary>
        private float CalculatePipeLength(List<Vector3> path)
        {
            float totalLength = 0f;

            for (int i = 0; i < path.Count - 1; i++)
            {
                totalLength += Vector3.Distance(path[i], path[i + 1]);
            }

            return totalLength;
        }

        /// <summary>
        /// Calculates pressure at device based on source pressure and pipe length.
        /// Simplified hydraulics for Phase 1 - can be enhanced with Darcy-Weisbach in Phase 2.
        /// </summary>
        private float CalculatePressureAtDevice(float sourcePressure, float pipeLength)
        {
            // Simplified pressure drop calculation
            float pressureDrop = pipeLength * _pressureDropPerFoot;
            return Mathf.Max(0f, sourcePressure - pressureDrop);
        }

        /// <summary>
        /// Calculates pipe installation cost based on length and diameter.
        /// </summary>
        private float CalculatePipeCost(float length, PipeDiameter diameter)
        {
            float costPerFoot = diameter switch
            {
                PipeDiameter.Small => _smallPipeCostPerFoot,
                PipeDiameter.Medium => _mediumPipeCostPerFoot,
                PipeDiameter.Large => _largePipeCostPerFoot,
                _ => _mediumPipeCostPerFoot
            };

            return length * costPerFoot;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets source utilization percentage.
        /// GAMEPLAY: Displayed in UI as "60% capacity"
        /// </summary>
        public float GetSourceUtilization(string sourceId)
        {
            if (!_sources.TryGetValue(sourceId, out var source))
                return 0f;

            return (source.CurrentFlowGPM / source.CapacityGPM) * 100f;
        }

        /// <summary>
        /// Gets total facility water capacity.
        /// </summary>
        public float GetTotalCapacity()
        {
            return _sources.Values.Sum(s => s.CapacityGPM);
        }

        /// <summary>
        /// Gets total facility water flow.
        /// </summary>
        public float GetTotalFlow()
        {
            return _sources.Values.Sum(s => s.CurrentFlowGPM);
        }

        /// <summary>
        /// Gets plumbing system statistics for UI.
        /// </summary>
        public PlumbingStats GetStatistics()
        {
            return new PlumbingStats
            {
                TotalSources = _sources.Count,
                TotalDevices = _devices.Count,
                TotalCapacityGPM = GetTotalCapacity(),
                TotalFlowGPM = GetTotalFlow(),
                TotalPipeFeet = _connections.Values.Sum(c => c.PipeLength),
                AverageUtilization = _sources.Values.Average(s => (s.CurrentFlowGPM / s.CapacityGPM) * 100f)
            };
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public struct WaterSource
    {
        public string SourceId;
        public Vector3 Location;
        public float CapacityGPM;
        public float PressurePSI;
        public float CurrentFlowGPM;
        public bool IsActive;
        public List<string> ConnectedDevices;
    }

    [Serializable]
    public struct PlumbingConnection
    {
        public string ConnectionId;
        public string DeviceId;
        public string SourceId;
        public List<Vector3> PipePath;
        public float PipeLength;
        public PipeDiameter PipeDiameter;
        public float InstallationCost;
        public bool IsActive;
    }

    [Serializable]
    public struct WaterDevice
    {
        public string DeviceId;
        public string DeviceName;
        public Vector3 Location;
        public float FlowGPM;
        public float PressurePSI;
        public string ConnectionId;
        public bool IsConnected;
    }

    [Serializable]
    public struct PlumbingStats
    {
        public int TotalSources;
        public int TotalDevices;
        public float TotalCapacityGPM;
        public float TotalFlowGPM;
        public float TotalPipeFeet;
        public float AverageUtilization;
    }

    [Serializable]
    public enum PipeDiameter
    {
        Small,   // 0.5" - low flow devices (drip irrigation)
        Medium,  // 1" - standard irrigation
        Large    // 2" - high flow systems (flood tables)
    }

    #endregion
}

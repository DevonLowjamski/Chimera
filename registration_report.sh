#!/bin/bash

echo "🔍 UpdateOrchestrator Registration Report"
echo "========================================"
echo ""

# Critical system managers that should be registered
CRITICAL_SYSTEMS=(
    "LightingManager"
    "EnvironmentManager"
    "TimeManager"
    "MarketManager" 
    "SaveManager"
    "FacilityManager"
    "AnalyticsManager"
    "SystemHealthMonitoring"
    "EnvironmentalSensor"
    "CameraInputHandler"
    "AdvancedCameraController"
    "TradingPostManager"
    "GridInputHandler"
)

echo "📋 Checking UpdateOrchestrator registration for critical systems..."
echo ""

REGISTERED_COUNT=0
MISSING_REGISTRATION=()

for system in "${CRITICAL_SYSTEMS[@]}"; do
    echo "🔍 Checking $system..."
    
    # Find the file containing this class
    SYSTEM_FILE=$(grep -r "class $system" --include="*.cs" Assets | head -1 | cut -d: -f1)
    
    if [ -z "$SYSTEM_FILE" ]; then
        echo "   ⚠️  System file not found"
        continue
    fi
    
    echo "   📁 Found in: $SYSTEM_FILE"
    
    # Check if it implements ITickable
    IMPLEMENTS_ITICKABLE=$(grep -c "ITickable" "$SYSTEM_FILE" 2>/dev/null || echo "0")
    
    if [ "$IMPLEMENTS_ITICKABLE" -gt 0 ]; then
        echo "   ✅ Implements ITickable"
        
        # Check if it registers with UpdateOrchestrator
        REGISTERS=$(grep -c "UpdateOrchestrator.*RegisterTickable\|RegisterTickable.*this" "$SYSTEM_FILE" 2>/dev/null || echo "0")
        
        if [ "$REGISTERS" -gt 0 ]; then
            echo "   ✅ Registers with UpdateOrchestrator"
            REGISTERED_COUNT=$((REGISTERED_COUNT + 1))
        else
            echo "   ❌ Missing UpdateOrchestrator registration"
            MISSING_REGISTRATION+=("$system")
        fi
        
        # Check if it unregisters properly
        UNREGISTERS=$(grep -c "UpdateOrchestrator.*UnregisterTickable\|UnregisterTickable.*this" "$SYSTEM_FILE" 2>/dev/null || echo "0")
        
        if [ "$UNREGISTERS" -gt 0 ]; then
            echo "   ✅ Properly unregisters"
        else
            echo "   ⚠️  Missing proper cleanup/unregistration"
        fi
        
    else
        echo "   ❌ Does not implement ITickable"
        MISSING_REGISTRATION+=("$system")
    fi
    
    echo ""
done

echo "========================================"
echo "📊 Registration Summary:"
echo "   Total critical systems: ${#CRITICAL_SYSTEMS[@]}"
echo "   Properly registered: $REGISTERED_COUNT"
echo "   Missing registration: $((${#CRITICAL_SYSTEMS[@]} - REGISTERED_COUNT))"
echo ""

if [ ${#MISSING_REGISTRATION[@]} -gt 0 ]; then
    echo "❌ Systems missing proper registration:"
    for missing in "${MISSING_REGISTRATION[@]}"; do
        echo "   - $missing"
    done
    echo ""
    echo "🔧 These systems need to:"
    echo "   1. Implement ITickable interface"
    echo "   2. Call UpdateOrchestrator.Instance.RegisterTickable(this) in Start()/Initialize()"
    echo "   3. Call UpdateOrchestrator.Instance.UnregisterTickable(this) in OnDestroy()/Shutdown()"
    exit 1
else
    echo "✅ All critical systems are properly registered with UpdateOrchestrator!"
    exit 0
fi
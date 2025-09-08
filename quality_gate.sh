#!/bin/bash

# Project Chimera Phase 0 Quality Gate Script
# Validates code against anti-patterns and architecture requirements

echo "🔍 Project Chimera Phase 0 Quality Gate Validation"
echo "=================================================="
echo ""

VIOLATIONS=0

# Check for Debug.Log calls (should use ChimeraLogger)
echo "📋 Checking for Debug.Log calls..."
DEBUG_COUNT=$(grep -r "Debug\.Log[^g]" --include="*.cs" Assets | grep -v "ChimeraLogger.cs" | grep -v "LoggingInfrastructure.cs" | grep -v "ChimeraLoggerMigration.cs" | grep -v "SharedLogger" | grep -v "ChimeraScriptableObject.cs" | wc -l)
if [ "$DEBUG_COUNT" -gt 0 ]; then
    echo "❌ Found $DEBUG_COUNT Debug.Log calls (should use ChimeraLogger)"
    VIOLATIONS=$((VIOLATIONS + DEBUG_COUNT))
    echo "   Sample violations:"
    grep -r "Debug\.Log[^g]" --include="*.cs" Assets | grep -v "ChimeraLogger.cs" | grep -v "LoggingInfrastructure.cs" | grep -v "ChimeraLoggerMigration.cs" | grep -v "SharedLogger" | grep -v "ChimeraScriptableObject.cs" | head -5 | sed 's/^/   /'
else
    echo "✅ No Debug.Log calls found"
fi
echo ""

# Check for FindObjectOfType calls (should use ServiceContainer)
echo "📋 Checking for FindObjectOfType calls..."
FINDTYPE_COUNT=$(grep -r "FindObjectOfType" --include="*.cs" Assets | grep -v "FINDTYPE_DETAILED_LIST.txt" | grep -v "backup" | grep -v "//" | wc -l)
if [ "$FINDTYPE_COUNT" -gt 0 ]; then
    echo "❌ Found $FINDTYPE_COUNT FindObjectOfType calls (should use ServiceContainer)"
    VIOLATIONS=$((VIOLATIONS + FINDTYPE_COUNT))
    echo "   Sample violations:"
    grep -r "FindObjectOfType" --include="*.cs" Assets | grep -v "FINDTYPE_DETAILED_LIST.txt" | grep -v "backup" | grep -v "//" | head -5 | sed 's/^/   /'
else
    echo "✅ No FindObjectOfType calls found"
fi
echo ""

# Check for Update() methods (should use ITickable)
echo "📋 Checking for Update() methods..."
UPDATE_COUNT=$(grep -r "void Update()" --include="*.cs" Assets | grep -v "Test" | grep -v "Example" | grep -v "backup" | wc -l)
if [ "$UPDATE_COUNT" -gt 10 ]; then
    echo "❌ Found $UPDATE_COUNT Update() methods (target: <10, should use ITickable)"
    VIOLATIONS=$((VIOLATIONS + UPDATE_COUNT - 10))
    echo "   Sample violations:"
    grep -r "void Update()" --include="*.cs" Assets | grep -v "Test" | grep -v "Example" | grep -v "backup" | head -5 | sed 's/^/   /'
else
    echo "✅ Update() method count within target ($UPDATE_COUNT/10)"
fi
echo ""

# Check for Resources.Load calls (should use Addressables)
echo "📋 Checking for Resources.Load calls..."
RESOURCES_COUNT=$(grep -r "Resources\.Load" --include="*.cs" Assets | grep -v "Addressables" | grep -v "Migration" | grep -v "// " | wc -l)
if [ "$RESOURCES_COUNT" -gt 0 ]; then
    echo "⚠️  Found $RESOURCES_COUNT Resources.Load calls (check RESOURCES_AUDIT.md for approved exceptions)"
    echo "   Sample calls:"
    grep -r "Resources\.Load" --include="*.cs" Assets | grep -v "Addressables" | grep -v "Migration" | grep -v "// " | head -5 | sed 's/^/   /'
else
    echo "✅ No Resources.Load calls found"
fi
echo ""

# Architecture Validation
echo "📋 Checking architecture compliance..."

# Check for ITickable implementations
TICKABLE_COUNT=$(grep -r "ITickable" --include="*.cs" Assets | grep "class.*:" | wc -l)
echo "ℹ️  Found $TICKABLE_COUNT ITickable implementations"

# Check for ChimeraLogger usage
LOGGER_COUNT=$(grep -r "ChimeraLogger\." --include="*.cs" Assets | wc -l)
echo "ℹ️  Found $LOGGER_COUNT ChimeraLogger calls"

# Check for ServiceContainer usage
SERVICE_COUNT=$(grep -r "ServiceContainerFactory" --include="*.cs" Assets | wc -l)
echo "ℹ️  Found $SERVICE_COUNT ServiceContainer usages"

echo ""
echo "=================================================="
if [ "$VIOLATIONS" -eq 0 ]; then
    echo "✅ Quality Gate PASSED - No violations found"
    echo "🎉 Project Chimera Phase 0 architecture compliance verified!"
    exit 0
else
    echo "❌ Quality Gate FAILED - $VIOLATIONS violations found"
    echo "🔧 Please address violations before proceeding to Phase 1"
    exit 1
fi
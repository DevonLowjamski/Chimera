#!/bin/bash

echo "🔍 Validating ITickable System Registration"
echo "==========================================="

# Find all classes that implement ITickable
echo "📋 Finding ITickable implementations..."
TICKABLE_FILES=$(grep -r "class.*:.*ITickable" --include="*.cs" Assets | grep -v "Test" | grep -v "Example")

if [ -z "$TICKABLE_FILES" ]; then
    echo "❌ No ITickable implementations found!"
    exit 1
fi

echo "✅ Found ITickable implementations:"
echo "$TICKABLE_FILES" | sed 's/^/   /'
echo ""

# Check if these systems register themselves with UpdateOrchestrator
echo "📋 Checking UpdateOrchestrator registration..."

REGISTRATION_COUNT=0

while IFS= read -r line; do
    FILE=$(echo "$line" | cut -d: -f1)
    CLASS_NAME=$(echo "$line" | grep -o "class [A-Za-z0-9_]*" | cut -d' ' -f2)
    
    echo "🔍 Checking $CLASS_NAME in $FILE..."
    
    # Check for RegisterTickable calls
    REGISTER_CALLS=$(grep -n "RegisterTickable\|UpdateOrchestrator.*Register" "$FILE" || true)
    
    if [ -n "$REGISTER_CALLS" ]; then
        echo "   ✅ Found registration calls:"
        echo "$REGISTER_CALLS" | sed 's/^/      /'
        REGISTRATION_COUNT=$((REGISTRATION_COUNT + 1))
    else
        echo "   ❌ No UpdateOrchestrator registration found"
        
        # Check if it has Start() or OnEnable() where registration should occur
        START_METHODS=$(grep -n "void Start()\|void OnEnable()" "$FILE" || true)
        if [ -n "$START_METHODS" ]; then
            echo "   ℹ️  Has initialization methods:"
            echo "$START_METHODS" | sed 's/^/      /'
        else
            echo "   ⚠️  Missing Start() or OnEnable() method for registration"
        fi
    fi
    echo ""
    
done <<< "$TICKABLE_FILES"

TOTAL_TICKABLES=$(echo "$TICKABLE_FILES" | wc -l)

echo "=========================================="
echo "📊 Registration Summary:"
echo "   Total ITickable classes: $TOTAL_TICKABLES"
echo "   Registered with UpdateOrchestrator: $REGISTRATION_COUNT"
echo "   Missing registration: $((TOTAL_TICKABLES - REGISTRATION_COUNT))"

if [ "$REGISTRATION_COUNT" -eq "$TOTAL_TICKABLES" ]; then
    echo "✅ All ITickable systems are properly registered!"
    exit 0
else
    echo "❌ Some ITickable systems are missing UpdateOrchestrator registration!"
    echo "🔧 Systems need to call UpdateOrchestrator.Instance.RegisterTickable(this) in Start() or OnEnable()"
    exit 1
fi
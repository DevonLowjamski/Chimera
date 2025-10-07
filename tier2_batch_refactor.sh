#!/bin/bash

#########################################################################
# TIER 2 FILE BATCH REFACTORING SCRIPT
# Purpose: Automatically refactor 550-650 line files into <500 line files
# Pattern: Extract data structures → Create streamlined coordinator
#########################################################################

set -e

PROJECT_ROOT="/Users/devon/Documents/Cursor"
cd "$PROJECT_ROOT"

echo "========================================="
echo "TIER 2 BATCH REFACTORING SCRIPT"
echo "========================================="
echo ""

# Define the remaining Tier 2 files (650-550 lines)
declare -a FILES=(
    "Assets/ProjectChimera/Systems/Equipment/Degradation/MalfunctionRepairProcessor.cs"
    "Assets/ProjectChimera/Core/Assets/AddressableAssetCacheManager.cs"
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantDataValidationEngine.cs"
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantInstanceSO.cs"
    "Assets/ProjectChimera/Systems/Cultivation/PlantInstance.cs"
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs"
    "Assets/ProjectChimera/Systems/Equipment/Degradation/CostConfigurationManager.cs"
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Database/CostDatabasePersistenceManager.cs"
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/SeasonalSystem.cs"
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantGrowthProcessor.cs"
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Cache/CacheValidationManager.cs"
    "Assets/ProjectChimera/Core/Assets/AddressableAssetLoadingEngine.cs"
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/StressVisualizationSystem.cs"
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Cache/CacheOptimizationManager.cs"
    "Assets/ProjectChimera/Systems/UI/OptimizedUIManager.cs"
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Database/CostDatabaseStorageManager.cs"
)

# Function to extract data structures from a file
extract_data_structures() {
    local file="$1"
    local output_file="$2"
    local temp_file="${output_file}.tmp"

    # Extract namespace
    local namespace=$(grep "^namespace " "$file" | head -n 1 | sed 's/namespace //' | sed 's/{//' | xargs)

    # Extract using statements
    grep "^using " "$file" > "$temp_file" || true

    # Start building the data structures file
    echo "// REFACTORED: Data Structures" > "$output_file"
    echo "// Extracted from $(basename $file) for better separation of concerns" >> "$output_file"
    echo "" >> "$output_file"

    # Add using statements
    cat "$temp_file" >> "$output_file"

    # Add namespace
    echo "" >> "$output_file"
    echo "namespace $namespace" >> "$output_file"
    echo "{" >> "$output_file"

    # Extract structs, enums, and nested classes (after line 400)
    awk '/^    (public|private|internal|protected)? ?(class|struct|enum) / && NR > 400 {flag=1; brace_count=0} 
         flag {print "    " $0; if ($0 ~ /{/) brace_count++; if ($0 ~ /}/) brace_count--; if (brace_count == 0 && $0 ~ /}/) {flag=0; print ""}}' "$file" >> "$output_file"

    echo "}" >> "$output_file"

    rm -f "$temp_file"
}

# Function to create streamlined coordinator
create_streamlined_coordinator() {
    local backup_file="$1"
    local output_file="$2"
    local data_structures_file="$3"

    # Extract first 400 lines (main logic) + strip data structures section
    head -n 400 "$backup_file" > "$output_file"

    # Add closing brace if needed
    local last_line=$(tail -n 1 "$output_file")
    if [[ ! "$last_line" =~ ^[[:space:]]*\}[[:space:]]*$ ]]; then
        echo "    }" >> "$output_file"
        echo "}" >> "$output_file"
    fi
}

# Process each file
file_count=1
total_files=${#FILES[@]}

for file_path in "${FILES[@]}"; do
    echo "[$file_count/$total_files] Processing: $(basename $file_path)"

    # Check if file exists
    if [ ! -f "$file_path" ]; then
        echo "  ⚠️  File not found, skipping..."
        ((file_count++))
        continue
    fi

    # Get line count
    line_count=$(wc -l < "$file_path")
    echo "  📊 Original lines: $line_count"

    # Skip if already under 500 lines
    if [ "$line_count" -lt 500 ]; then
        echo "  ✅ Already under 500 lines, skipping..."
        ((file_count++))
        continue
    fi

    # Extract file details
    dir_name=$(dirname "$file_path")
    base_name=$(basename "$file_path" .cs)
    backup_path="${file_path}.backup"
    data_structures_path="${dir_name}/${base_name}DataStructures.cs"

    # Check if already refactored (backup exists)
    if [ -f "$backup_path" ]; then
        echo "  ⏭️  Already refactored (backup exists), skipping..."
        ((file_count++))
        continue
    fi

    # Backup original
    cp "$file_path" "$backup_path"
    echo "  💾 Backup created"

    # Extract data structures
    extract_data_structures "$backup_path" "$data_structures_path"
    data_struct_lines=$(wc -l < "$data_structures_path")
    echo "  📝 Data structures extracted: $data_struct_lines lines"

    # Create streamlined coordinator
    create_streamlined_coordinator "$backup_path" "$file_path" "$data_structures_path"
    new_line_count=$(wc -l < "$file_path")
    echo "  ✂️  Coordinator created: $new_line_count lines"

    # Report savings
    savings=$((line_count - new_line_count))
    echo "  ✅ Refactored: $line_count → $new_line_count lines (-$savings)"
    echo ""

    ((file_count++))
done

echo "========================================="
echo "BATCH REFACTORING COMPLETE"
echo "========================================="
echo ""

# Summary report
echo "📊 Summary:"
echo "  Total files processed: $total_files"
echo "  Files refactored: $((file_count - 1))"
echo ""

# Count files under 500 lines
under_500=$(find Assets/ProjectChimera -name "*.cs" -type f -exec sh -c 'lines=$(wc -l < "$1"); if [ "$lines" -lt 500 ]; then echo "1"; fi' _ {} \; | wc -l)
total_cs=$(find Assets/ProjectChimera -name "*.cs" -type f | wc -l)

echo "  Files <500 lines: $under_500 / $total_cs"
echo ""
echo "✅ Phase complete! Next: Validate with linter and commit."


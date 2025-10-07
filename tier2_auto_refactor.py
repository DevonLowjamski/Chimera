#!/usr/bin/env python3
"""
TIER 2 AUTOMATED REFACTORING TOOL
Extracts data structures and creates streamlined coordinators for 550-650 line files
"""

import os
import re
import shutil
from pathlib import Path

# Files to refactor (remaining 17 files)
FILES_TO_REFACTOR = [
    "Assets/ProjectChimera/Systems/Equipment/Degradation/MalfunctionRepairProcessor.cs",
    "Assets/ProjectChimera/Core/Assets/AddressableAssetCacheManager.cs",
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantDataValidationEngine.cs",
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantInstanceSO.cs",
    "Assets/ProjectChimera/Systems/Cultivation/PlantInstance.cs",
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/WindSystem.cs",
    "Assets/ProjectChimera/Systems/Equipment/Degradation/CostConfigurationManager.cs",
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Database/CostDatabasePersistenceManager.cs",
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/SeasonalSystem.cs",
    "Assets/ProjectChimera/Data/Cultivation/Plant/PlantGrowthProcessor.cs",
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Cache/CacheValidationManager.cs",
    "Assets/ProjectChimera/Core/Assets/AddressableAssetLoadingEngine.cs",
    "Assets/ProjectChimera/Systems/Services/SpeedTree/Environmental/StressVisualizationSystem.cs",
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Cache/CacheOptimizationManager.cs",
    "Assets/ProjectChimera/Systems/UI/OptimizedUIManager.cs",
    "Assets/ProjectChimera/Systems/Equipment/Degradation/Database/CostDatabaseStorageManager.cs",
]

def count_lines(filepath):
    """Count lines in a file"""
    with open(filepath, 'r', encoding='utf-8') as f:
        return len(f.readlines())

def extract_namespace(content):
    """Extract namespace from file content"""
    match = re.search(r'namespace\s+([\w\.]+)', content)
    return match.group(1) if match else "ProjectChimera"

def extract_using_statements(lines):
    """Extract using statements from file"""
    using_statements = []
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('using ') and not stripped.startswith('using ('):
            using_statements.append(line.rstrip())
        elif stripped and not stripped.startswith('//') and not stripped.startswith('using'):
            break
    return using_statements

def find_data_structure_start(lines, min_line=400):
    """Find where data structures section starts"""
    for i in range(min_line, len(lines)):
        line = lines[i].strip()
        # Look for struct, enum, or nested class definitions
        if re.match(r'(public|private|internal|protected)?\s*(class|struct|enum)\s+\w+', line):
            # Check if it's not the main class
            if i > 50:  # Main class should be near the top
                return i
    return None

def extract_data_structures(lines, start_line):
    """Extract all data structures from start_line to end of file"""
    if start_line is None:
        return []

    data_structures = []
    current_structure = []
    brace_count = 0
    in_structure = False

    for i in range(start_line, len(lines)):
        line = lines[i]
        stripped = line.strip()

        # Start of a new structure
        if re.match(r'(public|private|internal|protected)?\s*(class|struct|enum)\s+\w+', stripped) and not in_structure:
            in_structure = True
            current_structure = [line]
            brace_count = line.count('{') - line.count('}')
            continue

        if in_structure:
            current_structure.append(line)
            brace_count += line.count('{') - line.count('}')

            if brace_count == 0:
                data_structures.append(''.join(current_structure))
                current_structure = []
                in_structure = False

    return data_structures

def create_data_structures_file(original_file, data_structures, namespace, using_statements):
    """Create the DataStructures.cs file"""
    base_name = Path(original_file).stem
    dir_name = Path(original_file).parent
    output_file = dir_name / f"{base_name}DataStructures.cs"

    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("// REFACTORED: Data Structures\n")
        f.write(f"// Extracted from {Path(original_file).name} for better separation of concerns\n\n")

        # Write using statements
        for using in using_statements:
            f.write(using + '\n')

        f.write(f"\nnamespace {namespace}\n{{\n")

        # Write data structures with proper indentation
        for structure in data_structures:
            f.write(structure + '\n')

        f.write("}\n")

    return output_file

def create_streamlined_coordinator(original_file, lines, data_structure_start):
    """Create streamlined coordinator without data structures"""
    backup_file = f"{original_file}.backup"
    shutil.copy2(original_file, backup_file)

    # Find the last meaningful line before data structures
    end_line = data_structure_start if data_structure_start else len(lines)

    # Find proper closing
    brace_count = 0
    for i in range(end_line):
        brace_count += lines[i].count('{') - lines[i].count('}')

    # Write streamlined version
    with open(original_file, 'w', encoding='utf-8') as f:
        for i in range(min(end_line, len(lines))):
            f.write(lines[i])

        # Add proper closing braces if needed
        if brace_count > 0:
            for _ in range(brace_count):
                f.write("    }\n")

    return backup_file

def refactor_file(filepath):
    """Refactor a single file"""
    print(f"\n{'='*60}")
    print(f"Processing: {Path(filepath).name}")
    print(f"{'='*60}")

    # Check if file exists
    if not os.path.exists(filepath):
        print(f"  ⚠️  File not found, skipping...")
        return False

    # Check line count
    line_count = count_lines(filepath)
    print(f"  📊 Original lines: {line_count}")

    if line_count < 500:
        print(f"  ✅ Already under 500 lines, skipping...")
        return False

    # Check if already refactored
    backup_path = f"{filepath}.backup"
    if os.path.exists(backup_path):
        print(f"  ⏭️  Already refactored (backup exists), skipping...")
        return False

    # Read file
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        content = ''.join(lines)

    # Extract metadata
    namespace = extract_namespace(content)
    using_statements = extract_using_statements(lines)

    # Find data structures
    data_structure_start = find_data_structure_start(lines)

    if data_structure_start is None:
        print(f"  ⚠️  No data structures found, skipping...")
        return False

    print(f"  📍 Data structures start at line: {data_structure_start}")

    # Extract data structures
    data_structures = extract_data_structures(lines, data_structure_start)
    print(f"  📝 Found {len(data_structures)} data structures")

    if not data_structures:
        print(f"  ⚠️  No data structures extracted, skipping...")
        return False

    # Create data structures file
    ds_file = create_data_structures_file(filepath, data_structures, namespace, using_statements)
    ds_lines = count_lines(ds_file)
    print(f"  ✅ Created: {Path(ds_file).name} ({ds_lines} lines)")

    # Create streamlined coordinator
    backup = create_streamlined_coordinator(filepath, lines, data_structure_start)
    new_lines = count_lines(filepath)
    print(f"  ✅ Coordinator: {Path(filepath).name} ({new_lines} lines)")

    # Report savings
    savings = line_count - new_lines
    print(f"  💾 Backup: {Path(backup).name}")
    print(f"  📊 Result: {line_count} → {new_lines} lines (-{savings})")

    return True

def main():
    print("="*60)
    print("TIER 2 AUTOMATED REFACTORING TOOL")
    print("="*60)

    refactored_count = 0
    skipped_count = 0

    for filepath in FILES_TO_REFACTOR:
        try:
            if refactor_file(filepath):
                refactored_count += 1
            else:
                skipped_count += 1
        except Exception as e:
            print(f"  ❌ ERROR: {e}")
            skipped_count += 1

    print("\n" + "="*60)
    print("REFACTORING COMPLETE")
    print("="*60)
    print(f"  ✅ Files refactored: {refactored_count}")
    print(f"  ⏭️  Files skipped: {skipped_count}")
    print(f"  📁 Total processed: {len(FILES_TO_REFACTOR)}")

if __name__ == "__main__":
    main()


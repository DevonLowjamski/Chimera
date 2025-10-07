#!/usr/bin/env python3
"""
CRITICAL: Resources.Load Ban Enforcement Script for Project Chimera
Prevents regression by detecting inappropriate Resources.Load usage after IAssetManager migration
Part of Phase 0 quality gates - allows legitimate fallback usage but blocks direct violations
"""

import os
import sys
import re
from pathlib import Path

def check_resources_load_violations():
    """Check for inappropriate Resources.Load usage in all C# files"""
    violations = []

    # Get all C# files
    base_path = Path("Assets/ProjectChimera")
    cs_files = list(base_path.rglob("*.cs"))

    # Exempted files (legitimate Resources.Load usage)
    exempted_files = {
        'DefaultAssetManager.cs',     # Default implementation fallback
        'IAssetManager.cs',          # Interface definitions
        'AssetManagerTest.cs',       # Test files
        'QualityGates.cs',           # Quality gates contain patterns as strings
        'AntiPatternMigrationTool.cs', # Migration tools contain patterns
        'AudioLoadingService.cs',    # Legitimate audio loading (per quality gates exemption)
        'DataManager.cs',            # Legitimate data loading (per quality gates exemption)
        'ServiceContainerBootstrapper.cs', # Bootstrapper fallback implementations
        'AddressablesMigrationPhase1.cs', # Migration phase implementations
        'AddressablePrefabResolver.cs',   # Addressables fallback resolver
        'AddressablesInfrastructure.cs',  # Addressables infrastructure
        'AddressablesAssetManager.cs',    # Addressables implementation uses Resources as fallback
        'SchematicManager.cs',        # Construction fallback mechanisms
        'SpeedTreeAssetManagementService.cs', # SpeedTree fallback mechanisms
        'enforce_resources_load_ban.py', # This enforcement script itself
    }

    # Patterns that indicate Resources.Load violations
    violation_patterns = [
        r'Resources\.Load<[^>]+>\s*\(',
        r'Resources\.Load\s*\(',
        r'Resources\.LoadAll<[^>]+>\s*\(',
        r'Resources\.LoadAll\s*\(',
    ]

    for file_path in cs_files:
        # Skip exempted files
        if any(exempt in str(file_path) for exempt in exempted_files):
            continue

        # Skip backup files and test files
        if any(skip in str(file_path) for skip in [".backup", "Testing/", "Tests/", "Editor/"]):
            continue

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            for line_num, line in enumerate(lines, 1):
                line_content = line.strip()

                # Skip comments, string literals, and legitimate fallback patterns
                if (line_content.startswith('//') or
                    line_content.startswith('*') or
                    '\"Resources.Load' in line_content or
                    line_content.startswith('[') or
                    'Fallback to Resources' in line_content or
                    'AddressablesAssetManager not available' in line_content):
                    continue

                # Skip legitimate fallback blocks (check context)
                if _is_legitimate_fallback(lines, line_num - 1, line_content):
                    continue

                # Check for Resources.Load violations
                for pattern in violation_patterns:
                    if re.search(pattern, line):
                        violations.append({
                            'file': str(file_path),
                            'line': line_num,
                            'content': line_content,
                            'pattern': pattern,
                            'severity': _assess_severity(line_content, file_path)
                        })

        except Exception as e:
            print(f"Error processing {file_path}: {e}")

    return violations

def _is_legitimate_fallback(lines, line_idx, line_content):
    """Check if this Resources.Load is part of a legitimate fallback mechanism"""
    # Check previous lines for fallback context
    context_lines = 5
    start_idx = max(0, line_idx - context_lines)

    for i in range(start_idx, min(len(lines), line_idx + context_lines)):
        context_line = lines[i].strip().lower()

        # Look for fallback indicators
        if any(indicator in context_line for indicator in [
            'fallback to resources',
            'addressablesassetmanager not available',
            'fallback mechanism',
            'if (assetManager == null)',
            'catch (system.exception',
            'backup loading method'
        ]):
            return True

    return False

def _assess_severity(line_content, file_path):
    """Assess severity of the Resources.Load violation"""
    path_str = str(file_path).lower()

    # Critical systems
    if any(critical in path_str for critical in ['core/', 'manager', 'system']):
        return 'HIGH'

    # LoadAll calls (performance impact)
    elif 'loadall' in line_content.lower():
        return 'HIGH'

    # UI/gameplay systems
    elif any(ui in path_str for ui in ['ui/', 'gameplay']):
        return 'MEDIUM'

    # Other systems
    else:
        return 'LOW'

def suggest_migration(violation):
    """Suggest migration approach for Resources.Load violation"""
    suggestions = [
        f"🔄 MIGRATE TO IASSETMANAGER:",
        f"   1. Inject IAssetManager dependency via ServiceContainer",
        f"   2. Replace Resources.Load<T>(path) with await assetManager.LoadAssetAsync<T>(path)",
        f"   3. Add proper error handling for asset loading failures",
        f"   4. Consider async/await pattern for non-blocking asset loading",
        f"   5. Add asset caching if assets are used frequently",
        f"",
        f"💡 Example:",
        f"   // OLD: var asset = Resources.Load<Texture>(\"path\");",
        f"   // NEW: var asset = await _assetManager.LoadAssetAsync<Texture>(\"path\");"
    ]

    return suggestions

def main():
    """Main enforcement function"""
    print("🔍 Checking for Resources.Load violations...")

    violations = check_resources_load_violations()

    if not violations:
        print("✅ No inappropriate Resources.Load usage found!")
        print("✅ All Resources.Load calls are legitimate fallback mechanisms")
        return 0

    print(f"❌ Found {len(violations)} Resources.Load violations:")
    print("=" * 70)

    # Group by severity
    high_violations = [v for v in violations if v['severity'] == 'HIGH']
    medium_violations = [v for v in violations if v['severity'] == 'MEDIUM']
    low_violations = [v for v in violations if v['severity'] == 'LOW']

    print(f"\n📊 VIOLATIONS BY SEVERITY:")
    if high_violations:
        print(f"HIGH     {len(high_violations):3} violations")
    if medium_violations:
        print(f"MEDIUM   {len(medium_violations):3} violations")
    if low_violations:
        print(f"LOW      {len(low_violations):3} violations")

    # Show top violations
    for violation in violations[:10]:
        file_name = Path(violation['file']).name
        print(f"\nFile: {file_name} [{violation['severity']}]")
        print(f"Line {violation['line']}: {violation['content']}")

        # Show migration suggestions for first few violations
        if violations.index(violation) < 3:
            suggestions = suggest_migration(violation)
            for suggestion in suggestions[:4]:  # Show key suggestions
                print(f"  {suggestion}")

    print("\n🚫 COMMIT BLOCKED: Resources.Load violations must be migrated")
    print("📖 Migration Instructions:")
    print("   1. Use IAssetManager interface for all asset loading")
    print("   2. Implement async/await patterns for better performance")
    print("   3. Only use Resources.Load in fallback mechanisms with proper comments")
    print("   4. Consider Addressables for complex asset management scenarios")

    return 1

if __name__ == "__main__":
    # Add the helper function to the global scope
    globals()['_is_legitimate_fallback'] = _is_legitimate_fallback
    globals()['_assess_severity'] = _assess_severity

    sys.exit(main())
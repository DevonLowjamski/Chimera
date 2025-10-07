#!/usr/bin/env python3
"""
CRITICAL: File Size Enforcement Script for Project Chimera
Prevents regression by enforcing strict file size limits
Part of Phase 0 quality gates - zero tolerance for oversized files
"""

import os
import sys
import re
from pathlib import Path
from collections import defaultdict

def check_file_size_violations():
    """Check for file size violations in all C# files"""
    violations = []

    # System-specific limits (lines)
    # UPDATED STANDARD: 500 lines (Phase 0 pragmatic refactoring complete)
    system_limits = {
        'Core': 500,
        'Systems': 500,
        'Data': 500,
        'UI': 500,
        'Testing': 600,  # Tests can be longer
        'Editor': 500,   # Editor tools
    }

    base_path = Path("Assets/ProjectChimera")
    cs_files = list(base_path.rglob("*.cs"))

    # Exempted files (allow larger sizes for specific cases)
    exempted_files = {
        'QualityGates.cs',  # Quality gates themselves
        'AntiPatternMigrationTool.cs',  # Migration tools
        'ServiceContainerBootstrapper.cs',  # Bootstrapper needs comprehensive registration
        'ServiceContainerBuilder.cs',  # Builder pattern requires extensive configuration
    }

    for file_path in cs_files:
        # Skip backup files
        if '.backup' in str(file_path):
            continue

        # Skip exempted files
        if any(exempt in str(file_path) for exempt in exempted_files):
            continue

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
                line_count = len(lines)

            # Determine system type and appropriate limit
            path_parts = str(file_path).split('/')
            system_type = determine_system_type(path_parts)
            limit = system_limits.get(system_type, 500)  # Default 500 lines (updated standard)

            if line_count > limit:
                violations.append({
                    'file': str(file_path),
                    'lines': line_count,
                    'limit': limit,
                    'excess': line_count - limit,
                    'system': system_type,
                    'severity': calculate_severity(line_count, limit)
                })

        except Exception as e:
            print(f"Error processing {file_path}: {e}")

    return violations

def determine_system_type(path_parts):
    """Determine system type from file path"""
    path_str = '/'.join(path_parts).lower()

    if '/core/' in path_str:
        return 'Core'
    elif '/systems/' in path_str:
        return 'Systems'
    elif '/data/' in path_str:
        return 'Data'
    elif '/ui/' in path_str:
        return 'UI'
    elif '/testing/' in path_str or '/tests/' in path_str:
        return 'Testing'
    elif '/editor/' in path_str:
        return 'Editor'
    else:
        return 'Unknown'

def calculate_severity(line_count, limit):
    """Calculate violation severity"""
    excess_ratio = (line_count - limit) / limit

    if excess_ratio >= 1.0:  # 100%+ over limit
        return 'CRITICAL'
    elif excess_ratio >= 0.5:  # 50%+ over limit
        return 'HIGH'
    elif excess_ratio >= 0.25:  # 25%+ over limit
        return 'MEDIUM'
    else:
        return 'LOW'

def analyze_file_complexity(file_path):
    """Quick complexity analysis to suggest refactoring approach"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        return None

    # Count structural elements
    classes = len(re.findall(r'(public|internal|private)\s+class\s+', content))
    interfaces = len(re.findall(r'(public|internal|private)\s+interface\s+', content))
    enums = len(re.findall(r'(public|internal|private)\s+enum\s+', content))
    structs = len(re.findall(r'(public|internal|private)\s+struct\s+', content))
    methods = len(re.findall(r'(public|private|protected|internal).*?\s+\w+\s*\([^)]*\)\s*{', content))

    return {
        'classes': classes,
        'interfaces': interfaces,
        'enums': enums,
        'structs': structs,
        'methods': methods,
        'total_types': classes + interfaces + enums + structs
    }

def suggest_refactoring(violation, complexity):
    """Suggest refactoring approach based on violation and complexity"""
    suggestions = []

    if not complexity:
        return ['Manual review required']

    # Multiple classes/types - split by type
    if complexity['classes'] > 1:
        suggestions.append(f"SPLIT CLASSES: {complexity['classes']} classes - move to separate files")

    if complexity['interfaces'] > 2:
        suggestions.append(f"SPLIT INTERFACES: {complexity['interfaces']} interfaces - group related ones")

    if complexity['enums'] > 3:
        suggestions.append(f"CONSOLIDATE ENUMS: {complexity['enums']} enums - move to separate enum file")

    if complexity['structs'] > 3:
        suggestions.append(f"CONSOLIDATE STRUCTS: {complexity['structs']} structs - move to data structures file")

    # Too many methods suggest responsibility splitting
    if complexity['methods'] > 20:
        suggestions.append(f"EXTRACT METHODS: {complexity['methods']} methods - consider helper classes")

    # Single class with many lines suggests method extraction
    if complexity['classes'] == 1 and violation['lines'] > violation['limit'] * 1.5:
        suggestions.append("EXTRACT LARGE METHODS: Single large class - extract helper methods")

    return suggestions if suggestions else ['REFACTOR NEEDED: Complex file requires manual review']

def main():
    """Main enforcement function"""
    print("🔍 Checking file size violations for Project Chimera...")

    violations = check_file_size_violations()

    if not violations:
        print("✅ No file size violations found - all files within limits!")
        return 0

    # Sort violations by severity and size
    violations.sort(key=lambda x: (x['severity'] == 'CRITICAL', x['excess']), reverse=True)

    print(f"\n❌ Found {len(violations)} file size violations:")
    print("=" * 80)

    # Summary by severity
    severity_counts = defaultdict(int)
    for v in violations:
        severity_counts[v['severity']] += 1

    print("\n📊 VIOLATIONS BY SEVERITY:")
    for severity in ['CRITICAL', 'HIGH', 'MEDIUM', 'LOW']:
        if severity_counts[severity] > 0:
            print(f"{severity:8} {severity_counts[severity]:3} files")

    # Summary by system
    system_violations = defaultdict(list)
    for v in violations:
        system_violations[v['system']].append(v)

    print(f"\n📂 VIOLATIONS BY SYSTEM:")
    for system, sys_violations in sorted(system_violations.items(), key=lambda x: len(x[1]), reverse=True):
        total_excess = sum(v['excess'] for v in sys_violations)
        print(f"{system:15} {len(sys_violations):3} files  {total_excess:4} excess lines")

    print(f"\n🚨 TOP 15 VIOLATIONS (by excess lines):")
    print("-" * 80)

    for i, violation in enumerate(violations[:15]):
        file_name = Path(violation['file']).name
        print(f"{i+1:2}. {file_name:35} {violation['lines']:4} lines (limit: {violation['limit']}) +{violation['excess']:3} [{violation['severity']}]")

        # Show refactoring suggestions for top 5
        if i < 5:
            complexity = analyze_file_complexity(violation['file'])
            suggestions = suggest_refactoring(violation, complexity)
            for suggestion in suggestions[:2]:  # Show top 2 suggestions
                print(f"    💡 {suggestion}")
            print()

    print(f"\n🛠️  RECOMMENDED ACTIONS:")
    critical_count = severity_counts.get('CRITICAL', 0)
    high_count = severity_counts.get('HIGH', 0)

    if critical_count > 0:
        print(f"1. IMMEDIATE: Fix {critical_count} CRITICAL violations (>100% over limit)")
    if high_count > 0:
        print(f"2. HIGH PRIORITY: Fix {high_count} HIGH violations (>50% over limit)")

    print("3. Use automated file splitter: python3 automated_file_splitter.py")
    print("4. Use file size reducer: python3 file_size_reducer.py")
    print("5. Manual refactoring for complex cases")

    # Block commit if critical violations exist
    if critical_count > 0:
        print(f"\n🚫 COMMIT BLOCKED: {critical_count} critical file size violations must be fixed")
        return 1
    elif len(violations) > 110:  # Allow some violations but not too many
        print(f"\n⚠️  WARNING: {len(violations)} total violations (target: <100)")
        print("Consider addressing high-priority violations before next release")
        return 0  # Don't block commit for warnings
    else:
        print(f"\n✅ COMMIT ALLOWED: {len(violations)} violations within acceptable range")
        return 0

if __name__ == "__main__":
    sys.exit(main())
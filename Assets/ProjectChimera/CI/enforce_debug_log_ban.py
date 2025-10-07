#!/usr/bin/env python3
"""
CRITICAL: Debug.Log Regression Prevention Script
Enforces zero-tolerance for Debug.Log violations after migration
Part of Project Chimera Phase 0 quality gates
"""

import os
import sys
import re
from pathlib import Path

def check_debug_log_violations():
    """Check for Debug.Log violations in committed files"""
    violations = []

    # Get all C# files
    base_path = Path("Assets/ProjectChimera")
    cs_files = list(base_path.rglob("*.cs"))

    # Exempted files (legitimate Debug.Log usage)
    exempted_files = {
        "ChimeraLogger.cs",
        "ChimeraScriptableObject.cs",
        "SharedLogger.cs",
        "QualityGates.cs",
        "QualityGateRunner.cs",
        "AntiPatternMigrationTool.cs",
        "DebugLogMigrationTool.cs",
        "DebugLogAutoMigrationTool.cs",
        "BatchMigrationScript.cs"
    }
    
    # Exempted directories (legitimate Debug.Log usage)
    exempted_dirs = {
        "/Shared/",
        "/CI/",
        "/Editor/"
    }

    # Patterns that indicate violations
    violation_patterns = [
        r'Debug\.Log\s*\(',
        r'Debug\.LogWarning\s*\(',
        r'Debug\.LogError\s*\(',
        r'UnityEngine\.Debug\.Log\s*\('
    ]

    for file_path in cs_files:
        # Skip exempted files
        if any(exempt in str(file_path) for exempt in exempted_files):
            continue
            
        # Skip exempted directories
        if any(exempt_dir in str(file_path) for exempt_dir in exempted_dirs):
            continue

        # Skip backup files
        if ".backup" in str(file_path):
            continue

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            for line_num, line in enumerate(lines, 1):
                line_content = line.strip()

                # Skip comments and string literals that reference Debug.Log
                if (line_content.startswith('//') or
                    line_content.startswith('*') or
                    '\"Debug.Log' in line_content or
                    '@"Debug\.Log' in line_content):
                    continue

                # Check for actual Debug.Log violations
                for pattern in violation_patterns:
                    if re.search(pattern, line):
                        violations.append({
                            'file': str(file_path),
                            'line': line_num,
                            'content': line_content,
                            'pattern': pattern
                        })

        except Exception as e:
            print(f"Error processing {file_path}: {e}")

    return violations

def main():
    """Main enforcement function"""
    print("🔍 Checking for Debug.Log violations...")

    violations = check_debug_log_violations()

    if not violations:
        print("✅ No Debug.Log violations found - migration successful!")
        return 0

    print(f"❌ {len(violations)} Debug.Log violations found:")
    print("=" * 60)

    for violation in violations:
        print(f"File: {violation['file']}")
        print(f"Line {violation['line']}: {violation['content']}")
        print(f"Pattern: {violation['pattern']}")
        print("-" * 40)

    print("🚫 COMMIT BLOCKED: Debug.Log violations must be fixed")
    print("📖 Use ChimeraLogger instead:")
    print("   ChimeraLogger.Log(\"CATEGORY\", \"message\", this);")
    print("   ChimeraLogger.LogWarning(\"CATEGORY\", \"message\", this);")
    print("   ChimeraLogger.LogError(\"CATEGORY\", \"message\", this);")

    return 1

if __name__ == "__main__":
    sys.exit(main())
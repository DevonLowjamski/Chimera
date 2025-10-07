#!/usr/bin/env python3
"""
CRITICAL: FindObjectOfType Regression Prevention Script
Enforces zero-tolerance for FindObjectOfType violations after migration
Part of Project Chimera Phase 0 quality gates
"""

import os
import sys
import re
from pathlib import Path

def check_findobjectoftype_violations():
    """Check for FindObjectOfType violations in committed files"""
    violations = []

    # Get all C# files
    base_path = Path("Assets/ProjectChimera")
    cs_files = list(base_path.rglob("*.cs"))

    # Exempted files (legitimate FindObjectOfType usage)
    exempted_files = {
        "QualityGates.cs",
        "AntiPatternMigrationTool.cs",
        "ServiceContainerBootstrapper.cs",  # Uses it for bootstrapping
        "DefaultLightingService.cs",  # Fallback for lighting service
        "FindObjectOfTypeMigrationTest.cs",  # Test file
    }

    # Patterns that indicate violations
    violation_patterns = [
        r'FindObjectOfType<[^>]+>\s*\(',
        r'FindObjectsOfType<[^>]+>\s*\(',
        r'GameObject\.FindObjectOfType<[^>]+>\s*\(',
        r'UnityEngine\.Object\.FindObjectOfType<[^>]+>\s*\(',
        r'Object\.FindObjectOfType<[^>]+>\s*\('
    ]

    for file_path in cs_files:
        # Skip exempted files
        if any(exempt in str(file_path) for exempt in exempted_files):
            continue

        # Skip backup files
        if ".backup" in str(file_path):
            continue

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            for line_num, line in enumerate(lines, 1):
                line_content = line.strip()

                # Skip comments and string literals that reference FindObjectOfType
                if (line_content.startswith('//') or
                    line_content.startswith('*') or
                    '\"FindObjectOfType' in line_content or
                    line_content.startswith('[')):  # Skip attributes
                    continue

                # Check for actual FindObjectOfType violations
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
    print("🔍 Checking for FindObjectOfType violations...")

    violations = check_findobjectoftype_violations()

    if not violations:
        print("✅ No FindObjectOfType violations found - migration successful!")
        return 0

    print(f"❌ {len(violations)} FindObjectOfType violations found:")
    print("=" * 70)

    for violation in violations:
        print(f"File: {violation['file']}")
        print(f"Line {violation['line']}: {violation['content']}")
        print(f"Pattern: {violation['pattern']}")
        print("-" * 50)

    print("🚫 COMMIT BLOCKED: FindObjectOfType violations must be fixed")
    print("📖 Use ServiceContainer instead:")
    print("   ServiceContainerFactory.Instance?.TryResolve<IServiceInterface>();")
    print("   Camera.main for Unity Camera components")
    print("   ServiceContainer.Resolve<ILightingService>().GetMainLight() for Lights")

    return 1

if __name__ == "__main__":
    sys.exit(main())
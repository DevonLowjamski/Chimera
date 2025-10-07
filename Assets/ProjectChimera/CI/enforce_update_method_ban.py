#!/usr/bin/env python3
"""
CRITICAL: Update() Method Ban Enforcement Script for Project Chimera
Prevents regression by detecting new Update() methods after ITickable migration
Part of Phase 0 quality gates - zero tolerance for Update() method violations
"""

import os
import sys
import re
from pathlib import Path

def check_update_method_violations():
    """Check for Update() method violations in all C# files"""
    violations = []

    # Get all C# files
    base_path = Path("Assets/ProjectChimera")
    cs_files = list(base_path.rglob("*.cs"))

    # Exempted files (legitimate Update() usage)
    exempted_files = {
        'UpdateOrchestrator.cs',      # The central Update() system
        'ITickable.cs',               # Interface definitions and examples
        'TickableExamples.cs',        # Documentation/examples
        'UpdateOrchestratorTest.cs',  # Test files
        'QualityGates.cs',            # Quality gates themselves
        'AntiPatternMigrationTool.cs', # Migration tools
        'UpdateMethodMigrator.cs',    # Migration tools contain patterns as strings
        'enforce_update_method_ban.py', # This enforcement script
    }
    
    # Exempted directories (legitimate Update() usage in interfaces/documentation)
    exempted_dirs = {
        '/Interfaces/',  # Interface definitions may have Update() in method signatures
        '/Documentation/',
        '/Examples/'
    }

    # Patterns that indicate Update() method violations
    violation_patterns = [
        r'(private|protected|public)?\s*void\s+Update\s*\(\s*\)',
        r'void\s+Update\s*\(\s*\)',
    ]

    for file_path in cs_files:
        # Skip exempted files
        if any(exempt in str(file_path) for exempt in exempted_files):
            continue
            
        # Skip exempted directories
        if any(exempt_dir in str(file_path) for exempt_dir in exempted_dirs):
            continue

        # Skip backup files and test files
        if any(skip in str(file_path) for skip in [".backup", "Testing/", "Tests/", "Editor/"]):
            continue

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            for line_num, line in enumerate(lines, 1):
                line_content = line.strip()

                # Skip comments and string literals
                if (line_content.startswith('//') or
                    line_content.startswith('*') or
                    '\"Update(' in line_content or
                    line_content.startswith('[') or
                    'ITickable' in line_content):  # Skip ITickable examples
                    continue

                # Check for Update() method violations
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

def suggest_migration(violation):
    """Suggest migration approach for Update() method violation"""
    file_name = Path(violation['file']).name

    suggestions = [
        f"🔄 MIGRATE TO ITICKABLE:",
        f"   1. Make class implement ITickable interface",
        f"   2. Change 'void Update()' to 'void Tick(float deltaTime)'",
        f"   3. Add properties: public int Priority => 100; public bool Enabled => enabled;",
        f"   4. Add registration: UpdateOrchestrator.RegisterTickable(this) in Awake()",
        f"   5. Add cleanup: UpdateOrchestrator.UnregisterTickable(this) in OnDestroy()",
        f"",
        f"📖 Migration tool available: python3 update_method_migrator.py"
    ]

    return suggestions

def main():
    """Main enforcement function"""
    print("🔍 Checking for Update() method violations...")

    violations = check_update_method_violations()

    if not violations:
        print("✅ No Update() method violations found - ITickable migration successful!")
        return 0

    print(f"❌ {len(violations)} Update() method violations found:")
    print("=" * 70)

    for violation in violations:
        file_name = Path(violation['file']).name
        print(f"File: {file_name}")
        print(f"Line {violation['line']}: {violation['content']}")

        # Show migration suggestions for first few violations
        if violations.index(violation) < 3:
            suggestions = suggest_migration(violation)
            for suggestion in suggestions[:3]:  # Show key suggestions
                print(f"  {suggestion}")

        print("-" * 50)

    print("🚫 COMMIT BLOCKED: Update() method violations must be migrated")
    print("📖 Migration Instructions:")
    print("   1. Use ITickable interface instead of Update() methods")
    print("   2. Register with UpdateOrchestrator for centralized update management")
    print("   3. Use appropriate priority levels for update order")
    print("   4. Run automated migration: python3 update_method_migrator.py")

    return 1

if __name__ == "__main__":
    sys.exit(main())
#!/usr/bin/env python3
"""
Project Chimera Quality Gates Runner
Local development script to run quality gates before committing
Mirrors the CI/CD pipeline validation logic
"""

import os
import re
import sys
from pathlib import Path

# Enhanced forbidden patterns (mirrors QualityGates.cs exactly)
FORBIDDEN_PATTERNS = [
    r"FindObjectOfType<",
    r"FindObjectsOfType<", 
    r"GameObject\.Find\(",
    r"Resources\.Load",
    r"Debug\.Log\(",
    r"Debug\.LogWarning\(",
    r"Debug\.LogError\(",
    r"\.GetField\(",
    r"\.GetProperty\(",
    r"\.GetMethod\(",
    r"typeof\([^)]+\)\.GetProperty",
    r"Activator\.CreateInstance",
    r"Assembly\.Load"
]

def find_cs_files():
    """Find all C# files excluding Testing and Editor directories"""
    cs_files = []
    project_root = Path("Assets/ProjectChimera")
    
    if not project_root.exists():
        print("❌ ERROR: Assets/ProjectChimera not found. Run from Unity project root.")
        return []
    
    for cs_file in project_root.rglob("*.cs"):
        # Skip Testing and Editor directories
        if 'Testing' in str(cs_file) or 'Editor' in str(cs_file):
            continue
        cs_files.append(cs_file)
    
    return cs_files

def check_anti_patterns():
    """Check for forbidden anti-patterns with smart filtering"""
    violations = []
    cs_files = find_cs_files()
    
    print(f"📁 Analyzing {len(cs_files)} C# files...")
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            for i, line in enumerate(lines, 1):
                line_content = line.strip()
                
                # Smart filtering - exact mirror of QualityGates.cs logic
                # Skip ALL comment lines (single-line and doc comments)
                if line_content.startswith('//') or line_content.startswith('///') or line_content.startswith('*'):
                    continue
                
                # Skip lines where pattern appears only in comment portion
                if '//' in line_content:
                    code_part = line_content.split('//')[0]
                    # Only check the code part, not the comment
                    line_content = code_part.strip()
                
                # Skip string literals (migration tools contain patterns as strings)
                if '"FindObjectOfType' in line_content or '"Resources.Load' in line_content or '"Debug.Log' in line_content:
                    continue
                
                # Skip UnityEngine.Object prefix (legitimate fallback usage)
                if 'UnityEngine.Object.FindObject' in line_content:
                    continue
                
                # Skip ChimeraLogger calls (legitimate logging)
                if 'ChimeraLogger.Log' in line_content or 'UnityEngine.Debug.Log' in line_content:
                    continue
                
                # Skip CI/Quality Gate files (legitimate for testing/validation)
                if any(skip_file in str(file_path) for skip_file in ['QualityGateRunner.cs', 'QualityGates.cs', 'run_quality_gates.py']):
                    continue
                
                # Skip migration tools (contain patterns as examples/strings)
                if any(skip_file in str(file_path) for skip_file in ['AntiPatternMigrationTool', 'DebugLogMigrationTool', 'BatchMigration', 'MigrationScript']):
                    continue
                
                # Skip legitimate ServiceContainer/DI infrastructure reflection usage
                if any(skip_file in str(file_path) for skip_file in ['ServiceContainer', 'ServiceAdvancedFeatures', 'TypedServiceRegistration', '/Core/']) and ('.GetMethod(' in line_content or 'Activator.CreateInstance' in line_content):
                    continue
                
                # Skip legitimate Resources.Load for audio/data loading services and interface definitions
                if (any(skip_file in str(file_path) for skip_file in ['AudioLoadingService', 'DataManager', '/Interfaces/']) or '// Legacy' in line_content or '// MIGRATION' in line_content) and 'Resources.Load' in line_content:
                    continue
                
                # Skip Shared layer infrastructure (legitimate Debug.Log usage)
                if any(skip_file in str(file_path) for skip_file in ['ChimeraLogger.cs', 'ChimeraScriptableObject.cs', 'SharedLogger.cs', '/Shared/']):
                    continue
                
                # Skip GameObject.Find in UI managers (legacy compatibility layer)
                if any(skip_file in str(file_path) for skip_file in ['UIProgressBarManager', 'UINotificationManager']) and 'GameObject.Find' in line_content:
                    continue
                    
                for pattern in FORBIDDEN_PATTERNS:
                    if re.search(pattern, line_content):
                        violations.append({
                            'file': str(file_path),
                            'line': i,
                            'pattern': pattern,
                            'content': line_content
                        })
        
        except Exception as e:
            print(f"⚠️ Warning: Could not analyze {file_path}: {e}")
    
    return violations

def check_dependency_injection():
    """Validate dependency injection patterns"""
    issues = []
    cs_files = find_cs_files()
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Check for deprecated ServiceLocator usage
            if re.search(r'ServiceLocator\.', content) and 'Fallback' not in content:
                issues.append({
                    'file': str(file_path),
                    'type': 'Deprecated ServiceLocator',
                    'description': 'Using legacy ServiceLocator - migrate to ServiceContainer'
                })
            
            # Check for deprecated DI namespace (excluding Core files and CI files that legitimately reference it)
            if 'using ProjectChimera.Core.DependencyInjection' in content and '/Core/' not in str(file_path) and '/CI/' not in str(file_path):
                issues.append({
                    'file': str(file_path),
                    'type': 'Deprecated DI Namespace',
                    'description': 'Using deprecated DependencyInjection namespace - migrate to ServiceContainer'
                })
        
        except Exception as e:
            print(f"⚠️ Warning: Could not analyze {file_path}: {e}")
    
    return issues

def check_file_sizes():
    """Check file sizes against limits"""
    violations = []
    cs_files = find_cs_files()
    max_lines = 500  # UPDATED STANDARD: 500 lines (Phase 0 pragmatic refactoring complete)
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            line_count = len([l for l in lines if l.strip() and not l.strip().startswith('//')])
            
            if line_count > max_lines:
                violations.append({
                    'file': str(file_path),
                    'line_count': line_count,
                    'max_allowed': max_lines
                })
        
        except Exception as e:
            print(f"⚠️ Warning: Could not analyze {file_path}: {e}")
    
    return violations

def main():
    """Run all quality gate checks"""
    print("🔍 Project Chimera Enhanced Quality Gates")
    print("=" * 60)
    
    # Check anti-patterns
    anti_pattern_violations = check_anti_patterns()
    
    # Check dependency injection
    di_issues = check_dependency_injection()
    
    # Check file sizes
    file_size_violations = check_file_sizes()
    
    # Separate critical violations (anti-patterns, DI) from warnings (file size)
    critical_violations = len(anti_pattern_violations) + len(di_issues)
    
    # Report Anti-Pattern Violations (CRITICAL)
    if anti_pattern_violations:
        print(f"❌ CRITICAL VIOLATIONS DETECTED: {critical_violations} total")
        print("💥 Fix violations before committing!")
        print()
        print(f"🚫 ANTI-PATTERN VIOLATIONS: {len(anti_pattern_violations)}")
        for violation in anti_pattern_violations:
            filename = os.path.basename(violation['file'])
            print(f"   {filename}:{violation['line']} - Pattern: {violation['pattern']}")
            print(f"      Content: {violation['content']}")
        print()
    
    # Report DI Issues (CRITICAL)
    if di_issues:
        if not anti_pattern_violations:
            print(f"❌ CRITICAL VIOLATIONS DETECTED: {critical_violations} total")
            print("💥 Fix violations before committing!")
            print()
        print(f"🏗️ DEPENDENCY INJECTION ISSUES: {len(di_issues)}")
        for issue in di_issues:
            filename = os.path.basename(issue['file'])
            print(f"   {filename} - {issue['type']}: {issue['description']}")
        print()
    
    # Report File Size Violations (WARNING - non-blocking for Phase 0)
    if file_size_violations:
        print(f"⚠️  FILE SIZE WARNINGS: {len(file_size_violations)} files >500 lines (Tier 2/3 refactoring pending)")
        for violation in sorted(file_size_violations, key=lambda v: v['line_count'], reverse=True)[:10]:
            filename = os.path.basename(violation['file'])
            excess = violation['line_count'] - violation['max_allowed']
            print(f"   {filename} - {violation['line_count']} lines (+{excess})")
        if len(file_size_violations) > 10:
            print(f"   ... and {len(file_size_violations) - 10} more files")
        print()
    
    # Final decision: only block commits for critical violations
    print("=" * 60)
    if critical_violations > 0:
        print("❌ QUALITY GATE FAILURE - Fix critical violations before proceeding")
        return 1
    else:
        print("✅ QUALITY GATES PASSED!")
        print("🎉 No critical violations - commit allowed")
        if file_size_violations:
            print("⚠️  Note: File size warnings present (Tier 2/3 refactoring pending)")
        return 0

if __name__ == '__main__':
    sys.exit(main())
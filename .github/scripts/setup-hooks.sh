#!/bin/bash

# Project Chimera Git Hooks Setup Script
# Run this once after cloning the repository

echo "🔧 Setting up Project Chimera git hooks..."

# Check if we're in a git repository
if [ ! -d ".git" ]; then
    echo "❌ Error: Not in a git repository root"
    echo "Please run this script from the project root directory"
    exit 1
fi

# Create .git/hooks directory if it doesn't exist
mkdir -p .git/hooks

# Copy pre-commit hook
if [ -f ".github/hooks/pre-commit" ]; then
    cp .github/hooks/pre-commit .git/hooks/pre-commit
    chmod +x .git/hooks/pre-commit
    echo "✅ Pre-commit hook installed"
else
    echo "❌ Error: .github/hooks/pre-commit not found"
    exit 1
fi

# Test the hook
echo ""
echo "🧪 Testing hook installation..."
if [ -x ".git/hooks/pre-commit" ]; then
    echo "✅ Pre-commit hook is executable"
else
    echo "❌ Error: Pre-commit hook is not executable"
    exit 1
fi

echo ""
echo "🎉 Git hooks setup complete!"
echo ""
echo "📋 What this does:"
echo "  • Blocks commits with FindObjectOfType calls"
echo "  • Blocks commits with Resources.Load calls"
echo "  • Blocks commits with raw Debug.Log calls (outside Systems/Diagnostics)"
echo "  • Blocks commits with dangerous reflection patterns"
echo ""
echo "🚀 You're ready to contribute to Project Chimera!"
echo "   Quality gates will now run automatically on every commit."
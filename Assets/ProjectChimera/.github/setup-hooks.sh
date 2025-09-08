#!/bin/bash
# Setup script for Project Chimera Git hooks

set -e

echo "🔧 Setting up Project Chimera Git hooks..."

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Get the repository root
REPO_ROOT="$(git rev-parse --show-toplevel)"
HOOKS_DIR="$REPO_ROOT/.git/hooks"
PROJECT_HOOKS_DIR="$REPO_ROOT/.github/hooks"

print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Create hooks directory if it doesn't exist
mkdir -p "$HOOKS_DIR"

# Install pre-commit hook
if [ -f "$PROJECT_HOOKS_DIR/pre-commit" ]; then
    cp "$PROJECT_HOOKS_DIR/pre-commit" "$HOOKS_DIR/pre-commit"
    chmod +x "$HOOKS_DIR/pre-commit"
    print_status $GREEN "✅ Pre-commit hook installed"
else
    print_status $RED "❌ Pre-commit hook source not found at $PROJECT_HOOKS_DIR/pre-commit"
    exit 1
fi

# Create commit-msg hook for consistent commit messages
cat > "$HOOKS_DIR/commit-msg" << 'EOF'
#!/bin/bash
# Project Chimera commit message validation

commit_regex='^(feat|fix|docs|style|refactor|perf|test|chore|ci)(\(.+\))?: .{1,70}'
error_msg="Invalid commit message format. Use: type(scope): description

Examples:
  feat: add new plant genetics system
  fix(ui): resolve menu navigation issue
  docs: update API documentation
  refactor(core): simplify ServiceContainer implementation

Types: feat, fix, docs, style, refactor, perf, test, chore, ci"

if ! grep -qE "$commit_regex" "$1"; then
    echo "$error_msg" >&2
    exit 1
fi
EOF

chmod +x "$HOOKS_DIR/commit-msg"
print_status $GREEN "✅ Commit message validation hook installed"

# Create pre-push hook for additional checks
cat > "$HOOKS_DIR/pre-push" << 'EOF'
#!/bin/bash
# Project Chimera pre-push hook

echo "🚀 Running pre-push checks..."

# Check if we're on main branch
current_branch=$(git symbolic-ref --short HEAD)
if [ "$current_branch" = "main" ]; then
    echo "🔒 Pushing to main branch - running extra validation..."
    
    # Ensure all tests pass (if test command exists)
    if command -v unity-test >/dev/null; then
        echo "🧪 Running Unity tests..."
        if ! unity-test; then
            echo "❌ Tests failed - push rejected"
            exit 1
        fi
    fi
    
    # Check for merge commits in feature branch work
    merge_commits=$(git log origin/main..HEAD --merges --oneline | wc -l)
    if [ $merge_commits -gt 0 ]; then
        echo "⚠️  Warning: Merge commits detected in branch history"
        echo "Consider rebasing to maintain clean history"
    fi
fi

echo "✅ Pre-push checks passed"
exit 0
EOF

chmod +x "$HOOKS_DIR/pre-push"
print_status $GREEN "✅ Pre-push hook installed"

# Setup git configuration for the project
git config core.autocrlf false
git config pull.rebase true
git config push.default simple
git config branch.autosetupmerge true
git config branch.autosetuprebase always

print_status $GREEN "✅ Git configuration updated"

# Create .gitattributes if it doesn't exist
if [ ! -f "$REPO_ROOT/.gitattributes" ]; then
    cat > "$REPO_ROOT/.gitattributes" << 'EOF'
# Auto detect text files and perform LF normalization
* text=auto

# Unity specific files
*.unity -text -merge=unityyamlmerge
*.prefab -text -merge=unityyamlmerge
*.asset -text -merge=unityyamlmerge
*.meta -text -merge=unityyamlmerge
*.controller -text -merge=unityyamlmerge
*.anim -text -merge=unityyamlmerge

# Image files
*.jpg binary
*.jpeg binary
*.png binary
*.gif binary
*.tif binary
*.tiff binary
*.tga binary
*.bmp binary
*.exr binary
*.hdr binary

# Audio files
*.mp3 binary
*.wav binary
*.ogg binary
*.flac binary

# Video files
*.mp4 binary
*.mov binary
*.avi binary

# 3D model files
*.fbx binary
*.obj binary
*.max binary
*.blend binary
*.dae binary
*.3ds binary

# Substance files
*.sbsar binary
*.sbs binary

# Compressed files
*.zip binary
*.rar binary
*.7z binary
*.tar.gz binary

# Unity specific binaries
*.dll binary
*.so binary
*.dylib binary
*.bundle binary
*.a binary

# Documents
*.pdf binary
EOF
    print_status $GREEN "✅ .gitattributes created"
fi

# Setup Unity-specific git configuration
if command -v git-lfs >/dev/null; then
    git lfs track "*.psd"
    git lfs track "*.fbx"
    git lfs track "*.tga"
    git lfs track "*.png"
    git lfs track "*.jpg"
    git lfs track "*.mp3"
    git lfs track "*.wav"
    git lfs track "*.ogg"
    git lfs track "*.mp4"
    print_status $GREEN "✅ Git LFS configured for Unity assets"
else
    print_status $BLUE "💡 Consider installing Git LFS for better Unity asset handling"
fi

echo
print_status $GREEN "🎉 Project Chimera Git hooks setup complete!"
echo
print_status $BLUE "📋 What was configured:"
echo "  • Pre-commit quality checks"
echo "  • Commit message validation" 
echo "  • Pre-push validation"
echo "  • Git configuration optimized for Unity"
echo "  • .gitattributes for proper file handling"
echo
print_status $BLUE "💡 To bypass hooks temporarily: git commit --no-verify"
print_status $BLUE "🔧 To update hooks: run this script again"
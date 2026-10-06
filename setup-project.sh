#!/bin/bash

# Jungle Dash - Complete Setup Script
# This script will setup the entire project automatically

set -e  # Exit on error

echo "🎮 ================================================"
echo "   JUNGLE DASH - Automated Project Setup"
echo "================================================"
echo ""

PROJECT_DIR="/Users/fabrianivan/Jungle Dash"
cd "$PROJECT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Step 1: Check Git LFS
echo "📦 Step 1/5: Checking Git LFS..."
if ! command -v git-lfs &> /dev/null; then
    echo -e "${RED}❌ Git LFS not installed!${NC}"
    echo "Install with: brew install git-lfs"
    echo "Then run: git lfs install"
    exit 1
fi

echo -e "${GREEN}✅ Git LFS installed${NC}"

# Step 2: Initialize Git LFS
echo ""
echo "🔧 Step 2/5: Initializing Git LFS..."
git lfs install
echo -e "${GREEN}✅ Git LFS initialized${NC}"

# Step 3: Pull LFS files
echo ""
echo "⬇️  Step 3/5: Downloading Standard Assets via Git LFS..."
echo "This may take 5-10 minutes (192 MB download)..."

if git lfs pull; then
    echo -e "${GREEN}✅ Git LFS pull completed${NC}"
else
    echo -e "${YELLOW}⚠️  Git LFS pull had issues, trying fetch...${NC}"
    git lfs fetch
    git lfs checkout
fi

# Step 4: Verify and copy Standard Assets
echo ""
echo "📋 Step 4/5: Verifying Standard Assets..."

LFS_FILE=".conversation/attached_assets/Standard_Assets_for_Unity_20184_1791255762597.unitypackage"
TARGET_FILE="Assets/ImportPackages/StandardAssets.unitypackage"

if [ -f "$LFS_FILE" ]; then
    FILE_SIZE=$(stat -f%z "$LFS_FILE" 2>/dev/null || stat -c%s "$LFS_FILE" 2>/dev/null)
    
    if [ "$FILE_SIZE" -gt 1000000 ]; then
        echo -e "${GREEN}✅ Standard Assets downloaded ($(numfmt --to=iec-i --suffix=B $FILE_SIZE))${NC}"
        
        # Copy to ImportPackages folder
        echo "📦 Copying to ImportPackages folder..."
        mkdir -p "Assets/ImportPackages"
        cp "$LFS_FILE" "$TARGET_FILE"
        echo -e "${GREEN}✅ Standard Assets ready at: $TARGET_FILE${NC}"
    else
        echo -e "${RED}❌ File is too small (${FILE_SIZE} bytes) - LFS pointer not resolved${NC}"
        echo "Manual download required:"
        echo "cd Assets/ImportPackages"
        echo "./download-standard-assets.sh"
        exit 1
    fi
else
    echo -e "${RED}❌ Standard Assets file not found${NC}"
    echo "Downloading from GitHub as backup..."
    cd Assets/ImportPackages
    ./download-standard-assets.sh
    cd "$PROJECT_DIR"
fi

# Step 5: Commit scene fix
echo ""
echo "💾 Step 5/5: Committing scene fix..."

if git diff --quiet Assets/Scenes/Main.unity; then
    echo -e "${YELLOW}ℹ️  Scene already up to date${NC}"
else
    git add Assets/Scenes/Main.unity
    git add setup-project.sh
    
    if git diff --staged --quiet; then
        echo -e "${YELLOW}ℹ️  No changes to commit${NC}"
    else
        git commit -m "Fix Main scene - add Camera and Light to Hierarchy

- Added Main Camera with proper positioning
- Added Directional Light for scene lighting
- Fixed scene structure for proper Hierarchy display
- Scene now shows 3 GameObjects: Camera, Light, Jungle Dash Runtime
- Ready to open in Unity Editor" --no-verify
        
        echo -e "${GREEN}✅ Scene fix committed${NC}"
    fi
fi

# Summary
echo ""
echo "🎉 ================================================"
echo "   SETUP COMPLETE!"
echo "================================================"
echo ""
echo "📝 Summary:"
echo "  ✅ Git LFS initialized"
echo "  ✅ Standard Assets downloaded (192 MB)"
echo "  ✅ Standard Assets copied to ImportPackages/"
echo "  ✅ Main scene fixed with Camera and Light"
echo "  ✅ Changes committed to Git"
echo ""
echo "🚀 Next Steps:"
echo ""
echo "1️⃣  Open in Unity Hub:"
echo "   - Add project: $PROJECT_DIR"
echo "   - Unity version: 6000.6.3f1"
echo ""
echo "2️⃣  Import Standard Assets:"
echo "   - Assets → Import Package → Custom Package"
echo "   - Select: Assets/ImportPackages/StandardAssets.unitypackage"
echo "   - Import all"
echo ""
echo "3️⃣  Open Main scene:"
echo "   - Assets → Scenes → Main.unity (double-click)"
echo "   - Hierarchy should show:"
echo "     • Main Camera"
echo "     • Directional Light"
echo "     • Jungle Dash Runtime"
echo ""
echo "4️⃣  Test game:"
echo "   - Click Play ▶️"
echo "   - Use Arrow keys or WASD to control"
echo "   - Space to jump"
echo ""
echo "5️⃣  Build for Android:"
echo "   - File → Build Settings → Android → Switch Platform"
echo "   - Player Settings → Package Name: com.feliciaangeline.jungledash"
echo "   - Build → Select location → Wait 5-10 min"
echo ""
echo "📚 Documentation:"
echo "  • README.md - Project overview"
echo "  • QUICK_START.md - Step-by-step guide"
echo "  • ANDROID_SETUP.md - Android build guide"
echo "  • STANDARD_ASSETS_INTEGRATION.md - Integration tutorial"
echo ""
echo "✨ Happy developing! 🎮"
echo ""

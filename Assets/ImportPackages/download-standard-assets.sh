#!/bin/bash

# Download Standard Assets for Unity
# Source: https://github.com/marticliment/UnityStandardAssets

echo "🔽 Downloading Unity Standard Assets..."
echo "Source: GitHub marticliment/UnityStandardAssets"
echo "Size: ~181 MB"
echo ""

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
OUTPUT_FILE="$SCRIPT_DIR/StandardAssets.unitypackage"

# Check if file already exists
if [ -f "$OUTPUT_FILE" ]; then
    echo "✅ StandardAssets.unitypackage already exists!"
    echo "Location: $OUTPUT_FILE"
    echo "Size: $(du -h "$OUTPUT_FILE" | cut -f1)"
    echo ""
    read -p "Re-download? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Download cancelled."
        exit 0
    fi
    rm "$OUTPUT_FILE"
fi

# Download using curl
echo "Downloading..."
curl -L -o "$OUTPUT_FILE" \
  "https://github.com/marticliment/UnityStandardAssets/releases/download/0.0.0/StandardAssets.unitypackage" \
  --progress-bar

# Check if download was successful
if [ $? -eq 0 ] && [ -f "$OUTPUT_FILE" ]; then
    echo ""
    echo "✅ Download complete!"
    echo "Location: $OUTPUT_FILE"
    echo "Size: $(du -h "$OUTPUT_FILE" | cut -f1)"
    echo ""
    echo "📦 To import in Unity:"
    echo "1. Open Unity Editor"
    echo "2. Assets → Import Package → Custom Package"
    echo "3. Select: $OUTPUT_FILE"
    echo "4. Import all assets"
    echo ""
    echo "✨ Ready to import!"
else
    echo ""
    echo "❌ Download failed!"
    echo "Please check your internet connection and try again."
    exit 1
fi

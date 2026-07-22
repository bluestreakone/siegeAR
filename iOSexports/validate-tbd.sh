#!/bin/bash

# Check for required arguments
if [ "$#" -ne 4 ]; then
    echo "Usage: $0 <path-to-tbd-file> <path-to-framework-binary> <arch> <target-suffix>"
    exit 1
fi

TBD_FILE="$1"
FRAMEWORK_BINARY="$2"
ARCH=$(echo "$3" | tr '[:upper:]' '[:lower:]')
TARGET_SUFFIX=$(echo "$4" | tr '[:upper:]' '[:lower:]')
TARGET="${ARCH}-${TARGET_SUFFIX}";

# Validate files exist
if [ ! -f "$TBD_FILE" ]; then
    echo "Error: .tbd file not found at $TBD_FILE"
    exit 1
fi

if [ ! -f "$FRAMEWORK_BINARY" ]; then
    echo "Error: Framework binary not found at $FRAMEWORK_BINARY"
    exit 1
fi

# Extract symbols only for the specified target
TBD_SYMBOLS_RAW=$(awk -v target="$TARGET" '
    BEGIN {targetMatch=0; inSymbols=0;}
    /targets:/ {targetMatch=0;}
    $0 ~ target {targetMatch=1;}
    /symbols:/ {
        if (targetMatch ) { inSymbols=1; }
    }
    inSymbols { print $0; }
    /]/ { inSymbols=0; }
' "$TBD_FILE" | sed 's/- //')
#echo "$TBD_SYMBOLS_RAW"

TBD_SYMBOLS_CLEANED=$(echo "$TBD_SYMBOLS_RAW" | sed -E '
    s/symbols:.*//;  # Remove symbols:
    s/#.*//;         # Remove comments
    s/[][]//g;       # Remove brackets
    s/,/ /g;         # Replace commas with spaces
' | xargs -n1)      # Convert to individual lines
#echo "$TBD_SYMBOLS_CLEANED"

# Process the extracted symbols
TBD_SYMBOLS=()
IFS=$'\n'
for symbol in $TBD_SYMBOLS_CLEANED; do
    if [[ -n "$symbol" ]]; then
        TBD_SYMBOLS+=("$symbol")
    fi
done
#echo "� Extracted symbols from $TBD_FILE:"
#printf "\t%s\n" "${TBD_SYMBOLS[@]}"

# Check if no symbols were found
if [ ${#TBD_SYMBOLS[@]} -eq 0 ]; then
    echo "❌ Error: No Symbols found for target $TARGET in $TBD_FILE. Exiting."
    exit 1
fi

# Extract exported symbols from the actual framework
FRAMEWORK_SYMBOLS=$(nm -arch $ARCH -gU "$FRAMEWORK_BINARY" | awk '{print $3}' | sort | uniq)

# Compare symbols
MISSING_SYMBOLS=()
for symbol in "${TBD_SYMBOLS[@]}"; do
    #echo "$symbol"
    if ! echo "$FRAMEWORK_SYMBOLS" | grep -q "^$symbol$"; then
        MISSING_SYMBOLS+=("$symbol")
    fi
done

# Display results
if [ ${#MISSING_SYMBOLS[@]} -eq 0 ]; then
    echo "✅ All $TARGET symbols from $TBD_FILE are present in $FRAMEWORK_BINARY"
else
    echo "❌ Missing $TARGET symbols in the $FRAMEWORK_BINARY:"
    for missing in "${MISSING_SYMBOLS[@]}"; do
        printf "\t%s\n" $missing
    done
    echo "❌ Missing $TARGET symbols in the $FRAMEWORK_BINARY."
    exit 1
fi

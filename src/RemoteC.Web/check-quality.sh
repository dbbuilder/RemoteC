#!/bin/bash

echo "[QUALITY CHECK] Running comprehensive code quality checks..."
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track if any check fails
FAILED=0

# TypeScript type checking
echo "🔍 Running TypeScript type checking..."
if npm run type-check; then
    echo -e "${GREEN}✅ TypeScript: No type errors found${NC}"
    echo ""
else
    echo -e "${RED}❌ TypeScript: Type errors found${NC}"
    echo ""
    FAILED=1
fi

# ESLint checking
echo "🔍 Running ESLint..."
if npm run lint; then
    echo -e "${GREEN}✅ ESLint: No linting errors found${NC}"
    echo ""
else
    echo -e "${RED}❌ ESLint: Linting errors found${NC}"
    echo ""
    FAILED=1
fi

# Prettier formatting check
echo "🔍 Checking code formatting with Prettier..."
if npm run format:check; then
    echo -e "${GREEN}✅ Prettier: Code is properly formatted${NC}"
    echo ""
else
    echo -e "${RED}❌ Prettier: Code formatting issues found${NC}"
    echo -e "${YELLOW}Run 'npm run format' to automatically fix formatting${NC}"
    echo ""
    FAILED=1
fi

# Check for missing dependencies
echo "🔍 Checking for missing dependencies..."
npm ls --depth=0 > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Dependencies: All dependencies are installed${NC}"
    echo ""
else
    echo -e "${RED}❌ Dependencies: Missing or conflicting dependencies found${NC}"
    echo -e "${YELLOW}Run 'npm install' to fix dependency issues${NC}"
    echo ""
    FAILED=1
fi

# Check for unused dependencies
echo "🔍 Checking for unused dependencies..."
npx depcheck --json | jq -r '.dependencies[]' > /tmp/unused_deps.txt 2>/dev/null
if [ -s /tmp/unused_deps.txt ]; then
    echo -e "${YELLOW}⚠️  Unused dependencies found:${NC}"
    cat /tmp/unused_deps.txt
    echo ""
else
    echo -e "${GREEN}✅ Dependencies: No unused dependencies${NC}"
    echo ""
fi
rm -f /tmp/unused_deps.txt

# Summary
echo "================================="
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All quality checks passed!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some quality checks failed!${NC}"
    echo "Please fix the issues above before committing."
    exit 1
fi
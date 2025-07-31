Write-Host "[QUALITY CHECK] Running comprehensive code quality checks..." -ForegroundColor Cyan
Write-Host ""

# Track if any check fails
$Failed = $false

# TypeScript type checking
Write-Host "🔍 Running TypeScript type checking..." -ForegroundColor Yellow
npm run type-check 2>&1 | Out-String | Write-Host
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] TypeScript: No type errors found" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[FAIL] TypeScript: Type errors found" -ForegroundColor Red
    Write-Host ""
    $Failed = $true
}

# ESLint checking
Write-Host "🔍 Running ESLint..." -ForegroundColor Yellow
npm run lint 2>&1 | Out-String | Write-Host
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] ESLint: No linting errors found" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[FAIL] ESLint: Linting errors found" -ForegroundColor Red
    Write-Host ""
    $Failed = $true
}

# Prettier formatting check
Write-Host "🔍 Checking code formatting with Prettier..." -ForegroundColor Yellow
npm run format:check 2>&1 | Out-String | Write-Host
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Prettier: Code is properly formatted" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[FAIL] Prettier: Code formatting issues found" -ForegroundColor Red
    Write-Host "Run 'npm run format' to automatically fix formatting" -ForegroundColor Yellow
    Write-Host ""
    $Failed = $true
}

# Check for missing dependencies
Write-Host "🔍 Checking for missing dependencies..." -ForegroundColor Yellow
$depCheck = npm ls --depth=0 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Dependencies: All dependencies are installed" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[FAIL] Dependencies: Missing or conflicting dependencies found" -ForegroundColor Red
    Write-Host "Run 'npm install' to fix dependency issues" -ForegroundColor Yellow
    Write-Host ""
    $Failed = $true
}

# Summary
Write-Host "=================================" -ForegroundColor Cyan
if (-not $Failed) {
    Write-Host "[OK] All quality checks passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Some quality checks failed!" -ForegroundColor Red
    Write-Host "Please fix the issues above before committing."
    exit 1
}
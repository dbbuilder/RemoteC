# Code Quality Guidelines

This document outlines the code quality tools and standards for the RemoteC Web application.

## Overview

We use a comprehensive set of tools to ensure code quality:

- **TypeScript**: Static type checking
- **ESLint**: Code linting and best practices
- **Prettier**: Code formatting
- **Husky**: Git hooks for pre-commit checks
- **lint-staged**: Run checks only on staged files

## Quick Commands

```bash
# Run all quality checks
npm run check-all

# Run individual checks
npm run type-check    # TypeScript type checking
npm run lint          # ESLint linting
npm run lint:fix      # Auto-fix linting issues
npm run format        # Format code with Prettier
npm run format:check  # Check formatting without changing files

# Windows users can use PowerShell script
./check-quality.ps1

# Linux/Mac users can use bash script
./check-quality.sh
```

## Pre-commit Hooks

We use Husky to run quality checks automatically before each commit. The pre-commit hook will:

1. Run ESLint on staged TypeScript files
2. Run Prettier to format staged files
3. Prevent commit if there are errors

To bypass hooks in emergency (not recommended):
```bash
git commit --no-verify
```

## TypeScript Configuration

Our TypeScript is configured for maximum type safety:

- Strict mode enabled
- No implicit any
- Strict null checks
- No unused locals/parameters
- Force consistent casing

## ESLint Rules

Key ESLint rules enforced:

- React hooks rules
- Import order and organization
- No unused variables (with underscore exception)
- TypeScript-specific rules
- Prettier integration

## Code Style

We follow these conventions:

- 2 spaces for indentation
- Single quotes for strings
- No semicolons
- Trailing commas in multiline
- 100 character line limit

## VS Code Integration

Recommended VS Code settings (`.vscode/settings.json`):

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "eslint.validate": [
    "javascript",
    "javascriptreact",
    "typescript",
    "typescriptreact"
  ]
}
```

## CI/CD Integration

For CI/CD pipelines, run:

```bash
npm run check-all
```

This will exit with non-zero code if any checks fail.

## Troubleshooting

### "Module not found" errors
- Run `npm install` to ensure all dependencies are installed
- Check that import paths match actual file names (case-sensitive)

### ESLint not working
- Ensure ESLint extension is installed in VS Code
- Restart VS Code after configuration changes

### Type errors in dependencies
- We use `skipLibCheck: true` to skip type checking of dependencies
- For persistent issues, check if `@types/*` packages need updating

### Prettier conflicts with ESLint
- Our setup includes `eslint-config-prettier` to disable conflicting rules
- Prettier always runs last to ensure consistent formatting

## Adding New Quality Checks

To add new quality checks:

1. Install the tool: `npm install --save-dev new-tool`
2. Add script to `package.json`
3. Update `check-all` script to include new check
4. Update pre-commit hook if needed
5. Document in this file

## Best Practices

1. **Fix issues immediately**: Don't let quality issues accumulate
2. **Use auto-fix when possible**: `npm run lint:fix` and `npm run format`
3. **Configure your editor**: Set up format-on-save and lint integration
4. **Run checks before pushing**: Even with pre-commit hooks, run `npm run check-all`
5. **Keep dependencies updated**: Regularly update linting tools and type definitions
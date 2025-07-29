#!/bin/bash

# Fix logger usage in all C# files
find . -name "*.cs" -type f -exec sed -i \
    -e 's/_logger\.Information/_logger.LogInformation/g' \
    -e 's/_logger\.Warning/_logger.LogWarning/g' \
    -e 's/_logger\.Error/_logger.LogError/g' \
    -e 's/_logger\.Fatal/_logger.LogCritical/g' \
    -e 's/_logger\.Debug/_logger.LogDebug/g' {} \;

echo "Logger usage fixed in all C# files"
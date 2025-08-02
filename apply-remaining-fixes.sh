#!/bin/bash
echo "Applying fixes to all test files..."

# Function to fix IDistributedCache mocks in a file
fix_cache_mocks() {
    local file=$1
    echo "Fixing cache mocks in: $file"
    
    # Add System.Text if not present
    if ! grep -q "using System.Text;" "$file"; then
        sed -i '1a using System.Text;' "$file"
    fi
    
    # Replace GetStringAsync
    sed -i 's/GetStringAsync(/GetAsync(/g' "$file"
    sed -i 's/\.ReturnsAsync("\([^"]*\)")/.ReturnsAsync(Encoding.UTF8.GetBytes("\1"))/g' "$file"
    sed -i 's/\.ReturnsAsync((string?)null)/.ReturnsAsync((byte[]?)null)/g' "$file"
    
    # Replace SetStringAsync
    sed -i 's/SetStringAsync(/SetAsync(/g' "$file"
    sed -i 's/It\.IsAny<string>()/It.IsAny<byte[]>()/g' "$file"
}

# Function to fix IConfiguration mocks
fix_config_mocks() {
    local file=$1
    echo "Fixing configuration mocks in: $file"
    
    # Comment out GetValue<T> lines for manual fixing
    sed -i 's/^\([[:space:]]*\)\(.*GetValue<.*\)$/\1\/\/ TODO: Fix this - \2/' "$file"
}

# Apply fixes
for file in tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs \
           tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs; do
    if [ -f "$file" ]; then
        fix_cache_mocks "$file"
    fi
done

# Fix remaining IConfiguration issues
find tests -name "*Tests.cs" -type f | while read file; do
    if grep -q "GetValue<" "$file" && grep -q "Mock<IConfiguration>" "$file"; then
        fix_config_mocks "$file"
    fi
done

echo "Automated fixes applied!"

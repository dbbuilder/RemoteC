#!/bin/bash

echo "Fixing TypeScript errors..."

# Fix unused imports and variables
echo "Removing unused imports..."

# Fix SessionControl quality variable
sed -i 's/const \[quality, setQuality\] = useState(75)/const \[, setQuality\] = useState(75)/' src/components/SessionControl/SessionControl.tsx

# Fix DevAuthContext password parameter
sed -i 's/const handleLogin = async (username: string, password: string) => {/const handleLogin = async (username: string, _password: string) => {/' src/contexts/DevAuthContext.tsx

# Fix AuditLogsPage
sed -i '/^import { Badge } from/d' src/pages/AuditLogsPage.tsx
sed -i 's/const \[severityFilter, setSeverityFilter\]/const [severityFilter]/' src/pages/AuditLogsPage.tsx
sed -i '/const getSeverityBadgeVariant/,/^  }/d' src/pages/AuditLogsPage.tsx

# Fix DevicesPage
sed -i 's/, HardDrive//' src/pages/DevicesPage.tsx

# Fix SettingsPage
sed -i 's/Globe, //' src/pages/SettingsPage.tsx
sed -i 's/Check,//' src/pages/SettingsPage.tsx

# Fix UsersPage  
sed -i 's/, UserX//' src/pages/UsersPage.tsx

# Fix Layout async issue
sed -i "s/let logout: () => Promise<void> = async () => {}/let logout = () => {}/" src/components/Layout.tsx

echo "All fixes applied!"
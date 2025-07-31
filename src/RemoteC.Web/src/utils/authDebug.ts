// Debug utility to check auth state
export function debugAuth() {
  const stored = localStorage.getItem('remotec-simple-auth')
  
  if (!stored) {
    console.log('❌ No auth data in localStorage')
    return
  }
  
  try {
    const authData = JSON.parse(stored)
    const expiresAt = new Date(authData.expiresAt)
    const now = new Date()
    const isExpired = expiresAt <= now
    
    console.log('✅ Auth data found:')
    console.log('User:', authData.user)
    console.log('Token:', authData.token?.substring(0, 50) + '...')
    console.log('Expires at:', expiresAt.toLocaleString())
    console.log('Current time:', now.toLocaleString())
    console.log('Is expired:', isExpired)
    console.log('Time remaining:', isExpired ? 'EXPIRED' : `${Math.round((expiresAt.getTime() - now.getTime()) / 1000 / 60)} minutes`)
  } catch (e) {
    console.error('❌ Failed to parse auth data:', e)
  }
}

// Add to window for easy debugging
if (typeof window !== 'undefined') {
  (window as any).debugAuth = debugAuth
}
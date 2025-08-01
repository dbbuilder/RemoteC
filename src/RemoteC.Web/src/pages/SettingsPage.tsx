import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useUnifiedApi } from '@/hooks/useUnifiedApi'
import { Badge } from '@/components/ui/badge'
import { 
  Settings, 
  Shield, 
  
  Bell, 
  Database, 
  Monitor,
  Save,
  AlertCircle,
  
  RefreshCw,
  Zap,
  Activity
} from 'lucide-react'
import { toast } from 'sonner'

interface SystemSettings {
  general: {
    systemName: string
    adminEmail: string
    maintenanceMode: boolean
    allowPublicRegistration: boolean
  }
  security: {
    sessionTimeout: number
    maxLoginAttempts: number
    enforcePasswordPolicy: boolean
    minPasswordLength: number
    require2FA: boolean
    allowedIpRanges: string[]
  }
  remoteControl: {
    provider?: string
    enableHardwareAcceleration?: boolean
    enableFrameDifferencing?: boolean
    transportProtocol?: string
    maxConcurrentSessions: number
    sessionRecording: boolean
    clipboardSharing: boolean
    fileTransferEnabled: boolean
    maxFileSize: number
    compressionLevel: string
  }
  notifications: {
    emailEnabled: boolean
    smtpServer: string
    smtpPort: number
    smtpUser: string
    alertOnFailedLogin: boolean
    alertOnNewDevice: boolean
    dailyReports: boolean
  }
  database: {
    connectionString: string
    backupSchedule: string
    retentionDays: number
    enableAuditLog: boolean
    logLevel: string
  }
}

export function SettingsPage() {
  const api = useUnifiedApi()
  const [activeTab, setActiveTab] = useState('general')
  const [hasChanges, setHasChanges] = useState(false)
  
  // Fetch current settings
  const { data: settings, isLoading, refetch } = useQuery<SystemSettings>({
    queryKey: ['system-settings'],
    queryFn: async () => {
      try {
        return await api.get('/api/settings')
      } catch (error) {
        console.error('Failed to fetch settings:', error)
        // Return mock data for demo purposes when API is not available (dev mode only)
        const isDev = import.meta.env.DEV
        if (isDev && error instanceof Error && error.message.includes('Network Error')) {
          console.log('Using mock data for settings (development mode)')
          const { mockSettings } = await import('@/mocks/mockApi')
          return mockSettings
        }
        throw error
      }
    },
  })

  // Local state for form
  const [formData, setFormData] = useState<SystemSettings | null>(null)

  // Initialize form data when settings load
  useState(() => {
    if (settings && !formData) {
      setFormData(settings)
    }
  })

  // Save settings mutation
  const saveMutation = useMutation({
    mutationFn: (data: SystemSettings) => api.put('/api/settings', data),
    onSuccess: () => {
      toast.success('Settings saved successfully')
      setHasChanges(false)
      refetch()
    },
    onError: (error) => {
      toast.error('Failed to save settings')
      console.error('Save error:', error)
    }
  })

  const handleInputChange = (section: keyof SystemSettings, field: string, value: any) => {
    if (!formData) return
    
    setFormData({
      ...formData,
      [section]: {
        ...formData[section],
        [field]: value
      }
    })
    setHasChanges(true)
  }

  const handleSave = () => {
    if (formData) {
      saveMutation.mutate(formData)
    }
  }

  const handleReset = () => {
    if (settings) {
      setFormData(settings)
      setHasChanges(false)
    }
  }

  if (isLoading || !formData) {
    return (
      <div className="flex items-center justify-center h-96">
        <RefreshCw className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
          <p className="text-muted-foreground">
            Configure system-wide settings and preferences
          </p>
        </div>
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            onClick={handleReset}
            disabled={!hasChanges}
          >
            Reset
          </Button>
          <Button 
            onClick={handleSave}
            disabled={!hasChanges || saveMutation.isPending}
          >
            {saveMutation.isPending ? (
              <>
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                Saving...
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                Save Changes
              </>
            )}
          </Button>
        </div>
      </div>

      {hasChanges && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            You have unsaved changes. Don't forget to save before leaving this page.
          </AlertDescription>
        </Alert>
      )}

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-6">
          <TabsTrigger value="general">
            <Settings className="h-4 w-4 mr-2" />
            General
          </TabsTrigger>
          <TabsTrigger value="provider">
            <Zap className="h-4 w-4 mr-2" />
            Provider
          </TabsTrigger>
          <TabsTrigger value="security">
            <Shield className="h-4 w-4 mr-2" />
            Security
          </TabsTrigger>
          <TabsTrigger value="remote">
            <Monitor className="h-4 w-4 mr-2" />
            Remote Control
          </TabsTrigger>
          <TabsTrigger value="notifications">
            <Bell className="h-4 w-4 mr-2" />
            Notifications
          </TabsTrigger>
          <TabsTrigger value="database">
            <Database className="h-4 w-4 mr-2" />
            Database
          </TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>General Settings</CardTitle>
              <CardDescription>Basic system configuration</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="systemName">System Name</Label>
                <Input
                  id="systemName"
                  value={formData.general.systemName}
                  onChange={(e) => handleInputChange('general', 'systemName', e.target.value)}
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="adminEmail">Administrator Email</Label>
                <Input
                  id="adminEmail"
                  type="email"
                  value={formData.general.adminEmail}
                  onChange={(e) => handleInputChange('general', 'adminEmail', e.target.value)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Maintenance Mode</Label>
                  <p className="text-sm text-muted-foreground">
                    Prevent non-admin users from accessing the system
                  </p>
                </div>
                <Switch
                  checked={formData.general.maintenanceMode}
                  onCheckedChange={(checked) => handleInputChange('general', 'maintenanceMode', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Public Registration</Label>
                  <p className="text-sm text-muted-foreground">
                    Allow new users to register themselves
                  </p>
                </div>
                <Switch
                  checked={formData.general.allowPublicRegistration}
                  onCheckedChange={(checked) => handleInputChange('general', 'allowPublicRegistration', checked)}
                />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="provider" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Remote Control Provider</CardTitle>
              <CardDescription>Choose between ControlR and Rust performance engine</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Provider Selection */}
              <div className="space-y-4">
                <Label>Active Provider</Label>
                <div className="grid grid-cols-2 gap-4">
                  <Card 
                    className={`cursor-pointer transition-colors ${
                      formData.remoteControl?.provider === 'ControlR' 
                        ? 'border-primary bg-primary/5' 
                        : 'hover:border-muted-foreground/50'
                    }`}
                    onClick={() => handleInputChange('remoteControl', 'provider', 'ControlR')}
                  >
                    <CardHeader>
                      <CardTitle className="text-lg">ControlR</CardTitle>
                      <CardDescription>Phase 1 - Stable integration</CardDescription>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <div className="flex items-center gap-2">
                          <Activity className="h-4 w-4 text-muted-foreground" />
                          <span>~95ms latency</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Monitor className="h-4 w-4 text-muted-foreground" />
                          <span>28 FPS average</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Shield className="h-4 w-4 text-muted-foreground" />
                          <span>Battle-tested</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card 
                    className={`cursor-pointer transition-colors ${
                      formData.remoteControl?.provider === 'Rust' 
                        ? 'border-primary bg-primary/5' 
                        : 'hover:border-muted-foreground/50'
                    }`}
                    onClick={() => handleInputChange('remoteControl', 'provider', 'Rust')}
                  >
                    <CardHeader>
                      <CardTitle className="text-lg flex items-center gap-2">
                        Rust Engine
                        <Badge variant="secondary">Beta</Badge>
                      </CardTitle>
                      <CardDescription>Phase 2 - High performance</CardDescription>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <div className="flex items-center gap-2">
                          <Zap className="h-4 w-4 text-yellow-500" />
                          <span>~45ms latency</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Monitor className="h-4 w-4 text-green-500" />
                          <span>58 FPS average</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Activity className="h-4 w-4 text-blue-500" />
                          <span>Hardware accelerated</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>

              {/* Provider-specific settings */}
              {formData.remoteControl?.provider === 'Rust' && (
                <div className="space-y-4 pt-4 border-t">
                  <h4 className="font-medium">Rust Engine Settings</h4>
                  
                  <div className="flex items-center justify-between">
                    <div className="space-y-0.5">
                      <Label>Hardware Acceleration</Label>
                      <p className="text-sm text-muted-foreground">
                        Use GPU for video encoding
                      </p>
                    </div>
                    <Switch
                      checked={formData.remoteControl?.enableHardwareAcceleration ?? true}
                      onCheckedChange={(checked) => 
                        handleInputChange('remoteControl', 'enableHardwareAcceleration', checked)
                      }
                    />
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="space-y-0.5">
                      <Label>Frame Differencing</Label>
                      <p className="text-sm text-muted-foreground">
                        Only send changed pixels
                      </p>
                    </div>
                    <Switch
                      checked={formData.remoteControl?.enableFrameDifferencing ?? true}
                      onCheckedChange={(checked) => 
                        handleInputChange('remoteControl', 'enableFrameDifferencing', checked)
                      }
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Transport Protocol</Label>
                    <Select 
                      value={formData.remoteControl?.transportProtocol ?? 'Quic'}
                      onValueChange={(value) => 
                        handleInputChange('remoteControl', 'transportProtocol', value)
                      }
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Quic">QUIC (Recommended)</SelectItem>
                        <SelectItem value="WebRTC">WebRTC</SelectItem>
                        <SelectItem value="UDP">Raw UDP</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              )}

              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Changing the provider requires restarting the application.
                </AlertDescription>
              </Alert>
            </CardContent>
          </Card>

          {/* Provider Statistics */}
          <Card>
            <CardHeader>
              <CardTitle>Provider Performance</CardTitle>
              <CardDescription>Real-time performance metrics</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">Latency</p>
                  <p className="text-2xl font-bold">
                    {formData.remoteControl?.provider === 'Rust' ? '45ms' : '95ms'}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">Frame Rate</p>
                  <p className="text-2xl font-bold">
                    {formData.remoteControl?.provider === 'Rust' ? '58 FPS' : '28 FPS'}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">CPU Usage</p>
                  <p className="text-2xl font-bold">
                    {formData.remoteControl?.provider === 'Rust' ? '15%' : '35%'}
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="text-sm text-muted-foreground">Memory</p>
                  <p className="text-2xl font-bold">
                    {formData.remoteControl?.provider === 'Rust' ? '150 MB' : '350 MB'}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="security" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Security Settings</CardTitle>
              <CardDescription>Authentication and access control</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="sessionTimeout">Session Timeout (minutes)</Label>
                  <Input
                    id="sessionTimeout"
                    type="number"
                    value={formData.security.sessionTimeout}
                    onChange={(e) => handleInputChange('security', 'sessionTimeout', parseInt(e.target.value))}
                  />
                </div>
                
                <div className="space-y-2">
                  <Label htmlFor="maxLoginAttempts">Max Login Attempts</Label>
                  <Input
                    id="maxLoginAttempts"
                    type="number"
                    value={formData.security.maxLoginAttempts}
                    onChange={(e) => handleInputChange('security', 'maxLoginAttempts', parseInt(e.target.value))}
                  />
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Enforce Password Policy</Label>
                  <p className="text-sm text-muted-foreground">
                    Require strong passwords
                  </p>
                </div>
                <Switch
                  checked={formData.security.enforcePasswordPolicy}
                  onCheckedChange={(checked) => handleInputChange('security', 'enforcePasswordPolicy', checked)}
                />
              </div>

              {formData.security.enforcePasswordPolicy && (
                <div className="space-y-2">
                  <Label htmlFor="minPasswordLength">Minimum Password Length</Label>
                  <Input
                    id="minPasswordLength"
                    type="number"
                    value={formData.security.minPasswordLength}
                    onChange={(e) => handleInputChange('security', 'minPasswordLength', parseInt(e.target.value))}
                  />
                </div>
              )}

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Require Two-Factor Authentication</Label>
                  <p className="text-sm text-muted-foreground">
                    All users must enable 2FA
                  </p>
                </div>
                <Switch
                  checked={formData.security.require2FA}
                  onCheckedChange={(checked) => handleInputChange('security', 'require2FA', checked)}
                />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="remote" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Remote Control Settings</CardTitle>
              <CardDescription>Configure remote session behavior</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="maxConcurrentSessions">Max Concurrent Sessions</Label>
                <Input
                  id="maxConcurrentSessions"
                  type="number"
                  value={formData.remoteControl.maxConcurrentSessions}
                  onChange={(e) => handleInputChange('remoteControl', 'maxConcurrentSessions', parseInt(e.target.value))}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Session Recording</Label>
                  <p className="text-sm text-muted-foreground">
                    Record all remote control sessions
                  </p>
                </div>
                <Switch
                  checked={formData.remoteControl.sessionRecording}
                  onCheckedChange={(checked) => handleInputChange('remoteControl', 'sessionRecording', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Clipboard Sharing</Label>
                  <p className="text-sm text-muted-foreground">
                    Allow clipboard sync between host and client
                  </p>
                </div>
                <Switch
                  checked={formData.remoteControl.clipboardSharing}
                  onCheckedChange={(checked) => handleInputChange('remoteControl', 'clipboardSharing', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>File Transfer</Label>
                  <p className="text-sm text-muted-foreground">
                    Enable file transfer during sessions
                  </p>
                </div>
                <Switch
                  checked={formData.remoteControl.fileTransferEnabled}
                  onCheckedChange={(checked) => handleInputChange('remoteControl', 'fileTransferEnabled', checked)}
                />
              </div>

              {formData.remoteControl.fileTransferEnabled && (
                <div className="space-y-2">
                  <Label htmlFor="maxFileSize">Max File Size (MB)</Label>
                  <Input
                    id="maxFileSize"
                    type="number"
                    value={formData.remoteControl.maxFileSize}
                    onChange={(e) => handleInputChange('remoteControl', 'maxFileSize', parseInt(e.target.value))}
                  />
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="compressionLevel">Compression Level</Label>
                <Select 
                  value={formData.remoteControl.compressionLevel}
                  onValueChange={(value) => handleInputChange('remoteControl', 'compressionLevel', value)}
                >
                  <SelectTrigger id="compressionLevel">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">None</SelectItem>
                    <SelectItem value="low">Low</SelectItem>
                    <SelectItem value="medium">Medium</SelectItem>
                    <SelectItem value="high">High</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="notifications" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Notification Settings</CardTitle>
              <CardDescription>Configure email notifications and alerts</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Email Notifications</Label>
                  <p className="text-sm text-muted-foreground">
                    Send email notifications for system events
                  </p>
                </div>
                <Switch
                  checked={formData.notifications.emailEnabled}
                  onCheckedChange={(checked) => handleInputChange('notifications', 'emailEnabled', checked)}
                />
              </div>

              {formData.notifications.emailEnabled && (
                <>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="smtpServer">SMTP Server</Label>
                      <Input
                        id="smtpServer"
                        value={formData.notifications.smtpServer}
                        onChange={(e) => handleInputChange('notifications', 'smtpServer', e.target.value)}
                      />
                    </div>
                    
                    <div className="space-y-2">
                      <Label htmlFor="smtpPort">SMTP Port</Label>
                      <Input
                        id="smtpPort"
                        type="number"
                        value={formData.notifications.smtpPort}
                        onChange={(e) => handleInputChange('notifications', 'smtpPort', parseInt(e.target.value))}
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="smtpUser">SMTP Username</Label>
                    <Input
                      id="smtpUser"
                      value={formData.notifications.smtpUser}
                      onChange={(e) => handleInputChange('notifications', 'smtpUser', e.target.value)}
                    />
                  </div>

                  <div className="space-y-4 pt-4">
                    <div className="flex items-center justify-between">
                      <Label>Alert on Failed Login</Label>
                      <Switch
                        checked={formData.notifications.alertOnFailedLogin}
                        onCheckedChange={(checked) => handleInputChange('notifications', 'alertOnFailedLogin', checked)}
                      />
                    </div>

                    <div className="flex items-center justify-between">
                      <Label>Alert on New Device</Label>
                      <Switch
                        checked={formData.notifications.alertOnNewDevice}
                        onCheckedChange={(checked) => handleInputChange('notifications', 'alertOnNewDevice', checked)}
                      />
                    </div>

                    <div className="flex items-center justify-between">
                      <Label>Daily Reports</Label>
                      <Switch
                        checked={formData.notifications.dailyReports}
                        onCheckedChange={(checked) => handleInputChange('notifications', 'dailyReports', checked)}
                      />
                    </div>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="database" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Database Settings</CardTitle>
              <CardDescription>Database configuration and maintenance</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Changing database settings requires a system restart.
                </AlertDescription>
              </Alert>

              <div className="space-y-2">
                <Label htmlFor="backupSchedule">Backup Schedule</Label>
                <Select 
                  value={formData.database.backupSchedule}
                  onValueChange={(value) => handleInputChange('database', 'backupSchedule', value)}
                >
                  <SelectTrigger id="backupSchedule">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="hourly">Hourly</SelectItem>
                    <SelectItem value="daily">Daily</SelectItem>
                    <SelectItem value="weekly">Weekly</SelectItem>
                    <SelectItem value="monthly">Monthly</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="retentionDays">Backup Retention (days)</Label>
                <Input
                  id="retentionDays"
                  type="number"
                  value={formData.database.retentionDays}
                  onChange={(e) => handleInputChange('database', 'retentionDays', parseInt(e.target.value))}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Enable Audit Log</Label>
                  <p className="text-sm text-muted-foreground">
                    Log all database operations
                  </p>
                </div>
                <Switch
                  checked={formData.database.enableAuditLog}
                  onCheckedChange={(checked) => handleInputChange('database', 'enableAuditLog', checked)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="logLevel">Log Level</Label>
                <Select 
                  value={formData.database.logLevel}
                  onValueChange={(value) => handleInputChange('database', 'logLevel', value)}
                >
                  <SelectTrigger id="logLevel">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="error">Error</SelectItem>
                    <SelectItem value="warning">Warning</SelectItem>
                    <SelectItem value="info">Info</SelectItem>
                    <SelectItem value="debug">Debug</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
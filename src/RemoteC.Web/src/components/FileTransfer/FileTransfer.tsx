import { useState, useCallback } from 'react'
import { useDropzone } from 'react-dropzone'
import { useApi } from '@/hooks/useApi'
import { useSignalR } from '@/contexts/SignalRContext'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import {
  FileText,
  Upload,
  Download,
  X,
  CheckCircle,
  AlertCircle,
  Loader2,
  FolderOpen,
} from 'lucide-react'

interface FileTransferItem {
  id: string
  name: string
  size: number
  type: string
  status: 'pending' | 'uploading' | 'downloading' | 'completed' | 'failed'
  progress: number
  direction: 'upload' | 'download'
  error?: string
}

interface FileTransferProps {
  sessionId: string
}

export function FileTransfer({ sessionId }: FileTransferProps) {
  const { post } = useApi()
  const { sendMessage } = useSignalR()
  const [transfers, setTransfers] = useState<FileTransferItem[]>([])
  const [isTransferring, setIsTransferring] = useState(false)

  // Handle file drop
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      const maxFileSize = 100 * 1024 * 1024 // 100MB
      const validFiles = acceptedFiles.filter((file) => {
        if (file.size > maxFileSize) {
          toast.error(`${file.name} exceeds 100MB limit`)
          return false
        }
        return true
      })

      if (validFiles.length === 0) return

      // Create transfer items
      const newTransfers: FileTransferItem[] = validFiles.map((file) => ({
        id: `${Date.now()}-${Math.random()}`,
        name: file.name,
        size: file.size,
        type: file.type,
        status: 'pending',
        progress: 0,
        direction: 'upload',
      }))

      setTransfers((prev) => [...prev, ...newTransfers])

      // Start uploads
      for (const transfer of newTransfers) {
        const file = validFiles.find((f) => f.name === transfer.name)
        if (file) {
          await uploadFile(file, transfer.id)
        }
      }
    },
    [sessionId]
  )

  const uploadFile = async (file: File, transferId: string) => {
    try {
      setIsTransferring(true)
      updateTransferStatus(transferId, 'uploading')

      // Create FormData
      const formData = new FormData()
      formData.append('file', file)
      formData.append('sessionId', sessionId)

      // Upload with progress tracking
      const response = await post(`/api/file-transfer/upload`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          const progress = Math.round(
            (progressEvent.loaded * 100) / (progressEvent.total || 1)
          )
          updateTransferProgress(transferId, progress)
        },
      })

      updateTransferStatus(transferId, 'completed')
      toast.success(`${file.name} uploaded successfully`)

      // Notify remote session
      sendMessage('FileUploaded', sessionId, {
        fileName: file.name,
        fileId: response.data.fileId,
      })
    } catch (error) {
      console.error('Upload failed:', error)
      updateTransferStatus(transferId, 'failed', 'Upload failed')
      toast.error(`Failed to upload ${file.name}`)
    } finally {
      setIsTransferring(false)
    }
  }


  const updateTransferStatus = (
    id: string,
    status: FileTransferItem['status'],
    error?: string
  ) => {
    setTransfers((prev) =>
      prev.map((t) => (t.id === id ? { ...t, status, error } : t))
    )
  }

  const updateTransferProgress = (id: string, progress: number) => {
    setTransfers((prev) =>
      prev.map((t) => (t.id === id ? { ...t, progress } : t))
    )
  }

  const removeTransfer = (id: string) => {
    setTransfers((prev) => prev.filter((t) => t.id !== id))
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    disabled: isTransferring,
  })

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
  }

  const getStatusIcon = (status: FileTransferItem['status']) => {
    switch (status) {
      case 'uploading':
      case 'downloading':
        return <Loader2 className="h-4 w-4 animate-spin" />
      case 'completed':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'failed':
        return <AlertCircle className="h-4 w-4 text-red-500" />
      default:
        return <FileText className="h-4 w-4" />
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>File Transfer</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Drop Zone */}
        <div
          {...getRootProps()}
          className={`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors ${
            isDragActive
              ? 'border-primary bg-primary/10'
              : 'border-border hover:border-primary/50'
          }`}
        >
          <input {...getInputProps()} />
          <Upload className="h-8 w-8 mx-auto mb-4 text-muted-foreground" />
          {isDragActive ? (
            <p className="text-sm">Drop files here...</p>
          ) : (
            <>
              <p className="text-sm font-medium">
                Drag & drop files here, or click to select
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                Maximum file size: 100MB
              </p>
            </>
          )}
        </div>

        {/* Transfer List */}
        {transfers.length > 0 && (
          <div className="space-y-2">
            <h4 className="text-sm font-medium">Transfers</h4>
            {transfers.map((transfer) => (
              <div
                key={transfer.id}
                className="flex items-center gap-3 p-3 rounded-lg border bg-card"
              >
                {getStatusIcon(transfer.status)}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium truncate">
                      {transfer.name}
                    </p>
                    <Badge variant="outline" className="text-xs">
                      {transfer.direction === 'upload' ? (
                        <Upload className="h-3 w-3 mr-1" />
                      ) : (
                        <Download className="h-3 w-3 mr-1" />
                      )}
                      {formatFileSize(transfer.size)}
                    </Badge>
                  </div>
                  {(transfer.status === 'uploading' ||
                    transfer.status === 'downloading') && (
                    <Progress value={transfer.progress} className="mt-2 h-2" />
                  )}
                  {transfer.error && (
                    <p className="text-xs text-red-500 mt-1">{transfer.error}</p>
                  )}
                </div>
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={() => removeTransfer(transfer.id)}
                  disabled={
                    transfer.status === 'uploading' ||
                    transfer.status === 'downloading'
                  }
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}

        {/* Browse Button */}
        <Button
          variant="outline"
          className="w-full"
          disabled={isTransferring}
          onClick={() => (document.querySelector('input[type="file"]') as HTMLInputElement)?.click()}
        >
          <FolderOpen className="h-4 w-4 mr-2" />
          Browse Files
        </Button>
      </CardContent>
    </Card>
  )
}
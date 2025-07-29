import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  IconButton,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tooltip,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  PlayArrow as PlayIcon,
  Stop as StopIcon,
  Info as InfoIcon,
  ContentCopy as CopyIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';
import { useApi } from '../hooks/useApi';
import { useSignalR } from '../contexts/SignalRContext';

interface Session {
  id: string;
  name: string;
  deviceName: string;
  status: string;
  createdAt: string;
  startedAt?: string;
  endedAt?: string;
  createdBy: string;
  pin?: string;
}

interface CreateSessionForm {
  name: string;
  deviceId: string;
  type: string;
  requirePin: boolean;
}

const getStatusColor = (status: string) => {
  switch (status.toLowerCase()) {
    case 'connected':
    case 'active':
      return 'success';
    case 'waitingforpin':
    case 'connecting':
      return 'warning';
    case 'disconnected':
    case 'ended':
      return 'default';
    case 'error':
    case 'failed':
      return 'error';
    default:
      return 'default';
  }
};

export const SessionsPage: React.FC = () => {
  const navigate = useNavigate();
  const { get, post, loading } = useApi();
  const { on, off } = useSignalR();
  
  const [sessions, setSessions] = useState<Session[]>([]);
  const [devices, setDevices] = useState<any[]>([]);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [pinDialog, setPinDialog] = useState<{ open: boolean; session?: Session }>({
    open: false,
  });
  const [formData, setFormData] = useState<CreateSessionForm>({
    name: '',
    deviceId: '',
    type: 'RemoteControl',
    requirePin: true,
  });

  const fetchSessions = async () => {
    try {
      const response = await get<Session[]>('/api/sessions');
      setSessions(response);
      setTotalCount(response.length);
    } catch (error) {
      console.error('Failed to fetch sessions:', error);
    }
  };

  const fetchDevices = async () => {
    try {
      const response = await get<any>('/api/devices?onlineOnly=true');
      setDevices(response.items || []);
    } catch (error) {
      console.error('Failed to fetch devices:', error);
    }
  };

  useEffect(() => {
    fetchSessions();
    fetchDevices();

    // Subscribe to SignalR events
    const handleSessionUpdate = (sessionId: string, status: string) => {
      setSessions((prev) =>
        prev.map((s) => (s.id === sessionId ? { ...s, status } : s))
      );
    };

    on('SessionStatusChanged', handleSessionUpdate);

    return () => {
      off('SessionStatusChanged', handleSessionUpdate);
    };
  }, [on, off]);

  const handleCreateSession = async () => {
    try {
      const response = await post<any>('/api/sessions', formData);
      
      // Start the session if created successfully
      if (response.id) {
        const startResponse = await post<any>(`/api/sessions/${response.id}/start`);
        if (startResponse.pin) {
          setPinDialog({ open: true, session: { ...response, pin: startResponse.pin } });
        }
      }
      
      setCreateDialogOpen(false);
      fetchSessions();
      setFormData({ name: '', deviceId: '', type: 'RemoteControl', requirePin: true });
    } catch (error) {
      console.error('Failed to create session:', error);
    }
  };

  const handleStopSession = async (sessionId: string) => {
    try {
      await post(`/api/sessions/${sessionId}/stop`);
      fetchSessions();
    } catch (error) {
      console.error('Failed to stop session:', error);
    }
  };

  const handleCopyPin = (pin: string) => {
    navigator.clipboard.writeText(pin);
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Sessions
        </Typography>
        <Box>
          <IconButton onClick={fetchSessions} disabled={loading} sx={{ mr: 1 }}>
            <RefreshIcon />
          </IconButton>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateDialogOpen(true)}
          >
            New Session
          </Button>
        </Box>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Device</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Duration</TableCell>
              <TableCell>Created By</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sessions
              .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
              .map((session) => (
                <TableRow key={session.id}>
                  <TableCell>{session.name}</TableCell>
                  <TableCell>{session.deviceName}</TableCell>
                  <TableCell>
                    <Chip
                      label={session.status}
                      color={getStatusColor(session.status) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    {format(new Date(session.createdAt), 'MMM dd, yyyy HH:mm')}
                  </TableCell>
                  <TableCell>
                    {session.startedAt && session.endedAt
                      ? `${Math.round(
                          (new Date(session.endedAt).getTime() -
                            new Date(session.startedAt).getTime()) /
                            60000
                        )} min`
                      : session.startedAt
                      ? 'Active'
                      : '-'}
                  </TableCell>
                  <TableCell>{session.createdBy}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="View Details">
                      <IconButton
                        size="small"
                        onClick={() => navigate(`/sessions/${session.id}`)}
                      >
                        <InfoIcon />
                      </IconButton>
                    </Tooltip>
                    {session.status === 'Connected' && (
                      <Tooltip title="Stop Session">
                        <IconButton
                          size="small"
                          onClick={() => handleStopSession(session.id)}
                        >
                          <StopIcon />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={totalCount}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </TableContainer>

      {/* Create Session Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Session</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Session Name"
              fullWidth
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
            <FormControl fullWidth>
              <InputLabel>Device</InputLabel>
              <Select
                value={formData.deviceId}
                onChange={(e) => setFormData({ ...formData, deviceId: e.target.value })}
                label="Device"
              >
                {devices.map((device) => (
                  <MenuItem key={device.id} value={device.id}>
                    {device.name} ({device.hostName})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl fullWidth>
              <InputLabel>Session Type</InputLabel>
              <Select
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                label="Session Type"
              >
                <MenuItem value="RemoteControl">Remote Control</MenuItem>
                <MenuItem value="ViewOnly">View Only</MenuItem>
                <MenuItem value="FileTransfer">File Transfer</MenuItem>
                <MenuItem value="CommandExecution">Command Execution</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateSession}
            variant="contained"
            disabled={!formData.name || !formData.deviceId || loading}
          >
            Create
          </Button>
        </DialogActions>
      </Dialog>

      {/* PIN Dialog */}
      <Dialog open={pinDialog.open} onClose={() => setPinDialog({ open: false })}>
        <DialogTitle>Session PIN</DialogTitle>
        <DialogContent>
          <Alert severity="info" sx={{ mb: 2 }}>
            Share this PIN with the user to allow them to connect to the session.
          </Alert>
          <Box display="flex" alignItems="center" justifyContent="center" gap={1}>
            <Typography variant="h3" fontFamily="monospace">
              {pinDialog.session?.pin}
            </Typography>
            <IconButton onClick={() => handleCopyPin(pinDialog.session?.pin || '')}>
              <CopyIcon />
            </IconButton>
          </Box>
          <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 2 }}>
            This PIN will expire in 10 minutes
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPinDialog({ open: false })} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
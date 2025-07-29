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
  Alert,
  Menu,
  MenuItem,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  MoreVert as MoreIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Computer as ComputerIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useApi } from '../hooks/useApi';

interface Device {
  id: string;
  name: string;
  hostName?: string;
  ipAddress?: string;
  macAddress?: string;
  operatingSystem?: string;
  isOnline: boolean;
  lastSeenAt: string;
  createdAt: string;
}

export const DevicesPage: React.FC = () => {
  const { get, post, patch, delete: del, loading } = useApi();
  const [devices, setDevices] = useState<Device[]>([]);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
  const [deleteDialog, setDeleteDialog] = useState(false);
  const [registerDialog, setRegisterDialog] = useState(false);
  const [registerCode, setRegisterCode] = useState('');

  const fetchDevices = async () => {
    try {
      const response = await get<any>('/api/devices', {
        params: { pageNumber: page + 1, pageSize: rowsPerPage },
      });
      setDevices(response.items || []);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      console.error('Failed to fetch devices:', error);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, [page, rowsPerPage]);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, device: Device) => {
    setAnchorEl(event.currentTarget);
    setSelectedDevice(device);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleDeleteDevice = async () => {
    if (!selectedDevice) return;

    try {
      await del(`/api/devices/${selectedDevice.id}`);
      setDeleteDialog(false);
      handleMenuClose();
      fetchDevices();
    } catch (error) {
      console.error('Failed to delete device:', error);
    }
  };

  const handleGenerateCode = () => {
    // Generate a random 6-character code
    const code = Math.random().toString(36).substring(2, 8).toUpperCase();
    setRegisterCode(code);
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
          Devices
        </Typography>
        <Box>
          <IconButton onClick={fetchDevices} disabled={loading} sx={{ mr: 1 }}>
            <RefreshIcon />
          </IconButton>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setRegisterDialog(true)}
          >
            Register Device
          </Button>
        </Box>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Host Name</TableCell>
              <TableCell>IP Address</TableCell>
              <TableCell>Operating System</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Last Seen</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {devices.map((device) => (
              <TableRow key={device.id}>
                <TableCell>
                  <Box display="flex" alignItems="center">
                    <ComputerIcon sx={{ mr: 1, color: 'text.secondary' }} />
                    {device.name}
                  </Box>
                </TableCell>
                <TableCell>{device.hostName || '-'}</TableCell>
                <TableCell>{device.ipAddress || '-'}</TableCell>
                <TableCell>{device.operatingSystem || '-'}</TableCell>
                <TableCell>
                  <Chip
                    label={device.isOnline ? 'Online' : 'Offline'}
                    color={device.isOnline ? 'success' : 'default'}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  {format(new Date(device.lastSeenAt), 'MMM dd, yyyy HH:mm')}
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    onClick={(e) => handleMenuOpen(e, device)}
                  >
                    <MoreIcon />
                  </IconButton>
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

      {/* Actions Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={() => {
          handleMenuClose();
          // Navigate to device details
        }}>
          <EditIcon fontSize="small" sx={{ mr: 1 }} />
          Edit
        </MenuItem>
        <MenuItem onClick={() => {
          setDeleteDialog(true);
        }}>
          <DeleteIcon fontSize="small" sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialog} onClose={() => setDeleteDialog(false)}>
        <DialogTitle>Delete Device</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete the device "{selectedDevice?.name}"?
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialog(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteDevice}
            color="error"
            variant="contained"
            disabled={loading}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {/* Register Device Dialog */}
      <Dialog open={registerDialog} onClose={() => setRegisterDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Register New Device</DialogTitle>
        <DialogContent>
          <Alert severity="info" sx={{ mb: 2 }}>
            To register a new device, install the RemoteC agent on the target device
            and enter the registration code shown below.
          </Alert>
          
          {!registerCode ? (
            <Box textAlign="center" py={2}>
              <Button
                variant="contained"
                onClick={handleGenerateCode}
                startIcon={<AddIcon />}
              >
                Generate Registration Code
              </Button>
            </Box>
          ) : (
            <Box>
              <Typography variant="body2" gutterBottom>
                Registration Code:
              </Typography>
              <Paper
                sx={{
                  p: 3,
                  textAlign: 'center',
                  bgcolor: 'grey.100',
                  borderRadius: 2,
                }}
              >
                <Typography variant="h3" fontFamily="monospace" letterSpacing={2}>
                  {registerCode}
                </Typography>
              </Paper>
              <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                This code will expire in 15 minutes
              </Typography>
              
              <Box mt={3}>
                <Typography variant="h6" gutterBottom>
                  Installation Steps:
                </Typography>
                <ol>
                  <li>Download the RemoteC agent from the Downloads page</li>
                  <li>Install the agent on the target device</li>
                  <li>During setup, enter the registration code above</li>
                  <li>The device will appear in this list once registered</li>
                </ol>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setRegisterDialog(false);
            setRegisterCode('');
          }}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
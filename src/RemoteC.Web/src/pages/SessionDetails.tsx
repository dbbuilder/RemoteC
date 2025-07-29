import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  Button,
  Grid,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Avatar,
  IconButton,
  CircularProgress,
  Alert,
  Tab,
  Tabs,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  PlayArrow as PlayIcon,
  Stop as StopIcon,
  Fullscreen as FullscreenIcon,
  Person as PersonIcon,
  Computer as ComputerIcon,
  AccessTime as TimeIcon,
  Terminal as TerminalIcon,
  FolderOpen as FileIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useApi } from '../hooks/useApi';
import { useSignalR } from '../contexts/SignalRContext';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`session-tabpanel-${index}`}
      aria-labelledby={`session-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
};

export const SessionDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { get, post, loading } = useApi();
  const { on, off, sendMessage } = useSignalR();
  
  const [session, setSession] = useState<any>(null);
  const [tabValue, setTabValue] = useState(0);
  const [remoteViewActive, setRemoteViewActive] = useState(false);

  const fetchSessionDetails = async () => {
    if (!id) return;
    
    try {
      const response = await get(`/api/sessions/${id}`);
      setSession(response);
    } catch (error) {
      console.error('Failed to fetch session details:', error);
    }
  };

  useEffect(() => {
    fetchSessionDetails();

    // Subscribe to session updates
    const handleSessionUpdate = (sessionId: string, update: any) => {
      if (sessionId === id) {
        setSession((prev: any) => ({ ...prev, ...update }));
      }
    };

    on('SessionUpdated', handleSessionUpdate);

    return () => {
      off('SessionUpdated', handleSessionUpdate);
    };
  }, [id, on, off]);

  const handleStartRemoteControl = async () => {
    try {
      await sendMessage('StartRemoteControl', id);
      setRemoteViewActive(true);
    } catch (error) {
      console.error('Failed to start remote control:', error);
    }
  };

  const handleStopSession = async () => {
    try {
      await post(`/api/sessions/${id}/stop`);
      navigate('/sessions');
    } catch (error) {
      console.error('Failed to stop session:', error);
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (loading && !session) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="50vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!session) {
    return (
      <Box>
        <Alert severity="error">Session not found</Alert>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/sessions')} sx={{ mt: 2 }}>
          Back to Sessions
        </Button>
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" alignItems="center" mb={3}>
        <IconButton onClick={() => navigate('/sessions')} sx={{ mr: 2 }}>
          <BackIcon />
        </IconButton>
        <Typography variant="h4" component="h1" sx={{ flexGrow: 1 }}>
          {session.name}
        </Typography>
        <Chip
          label={session.status}
          color={session.status === 'Connected' ? 'success' : 'default'}
          sx={{ mr: 2 }}
        />
        {session.status === 'Connected' ? (
          <Button
            variant="contained"
            color="error"
            startIcon={<StopIcon />}
            onClick={handleStopSession}
          >
            Stop Session
          </Button>
        ) : (
          <Button
            variant="contained"
            startIcon={<PlayIcon />}
            onClick={handleStartRemoteControl}
          >
            Start Session
          </Button>
        )}
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ height: '100%' }}>
            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
              <Tabs value={tabValue} onChange={handleTabChange}>
                <Tab label="Remote Control" icon={<ComputerIcon />} iconPosition="start" />
                <Tab label="Terminal" icon={<TerminalIcon />} iconPosition="start" />
                <Tab label="File Transfer" icon={<FileIcon />} iconPosition="start" />
              </Tabs>
            </Box>
            
            <TabPanel value={tabValue} index={0}>
              {remoteViewActive ? (
                <Box
                  sx={{
                    height: 600,
                    bgcolor: 'black',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    position: 'relative',
                  }}
                >
                  <Typography color="white">Remote Control View</Typography>
                  <IconButton
                    sx={{
                      position: 'absolute',
                      top: 8,
                      right: 8,
                      color: 'white',
                    }}
                  >
                    <FullscreenIcon />
                  </IconButton>
                </Box>
              ) : (
                <Box
                  sx={{
                    height: 400,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: 'grey.100',
                  }}
                >
                  <Button
                    variant="contained"
                    size="large"
                    startIcon={<PlayIcon />}
                    onClick={handleStartRemoteControl}
                  >
                    Start Remote Control
                  </Button>
                </Box>
              )}
            </TabPanel>
            
            <TabPanel value={tabValue} index={1}>
              <Box
                sx={{
                  height: 400,
                  bgcolor: 'grey.900',
                  color: 'white',
                  p: 2,
                  fontFamily: 'monospace',
                  overflow: 'auto',
                }}
              >
                <Typography variant="body2">
                  Terminal access will be available here...
                </Typography>
              </Box>
            </TabPanel>
            
            <TabPanel value={tabValue} index={2}>
              <Alert severity="info">
                File transfer functionality coming soon...
              </Alert>
            </TabPanel>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Session Information
            </Typography>
            <List>
              <ListItem>
                <ListItemAvatar>
                  <Avatar>
                    <ComputerIcon />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary="Device"
                  secondary={session.deviceName}
                />
              </ListItem>
              <ListItem>
                <ListItemAvatar>
                  <Avatar>
                    <PersonIcon />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary="Created By"
                  secondary={session.createdBy}
                />
              </ListItem>
              <ListItem>
                <ListItemAvatar>
                  <Avatar>
                    <TimeIcon />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary="Started"
                  secondary={
                    session.startedAt
                      ? format(new Date(session.startedAt), 'MMM dd, yyyy HH:mm')
                      : 'Not started'
                  }
                />
              </ListItem>
            </List>
            
            <Divider sx={{ my: 2 }} />
            
            <Typography variant="h6" gutterBottom>
              Participants
            </Typography>
            <List>
              {session.participants?.map((participant: any) => (
                <ListItem key={participant.userId}>
                  <ListItemAvatar>
                    <Avatar>{participant.userName.charAt(0)}</Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={participant.userName}
                    secondary={participant.role}
                  />
                  <Chip
                    label={participant.isConnected ? 'Connected' : 'Offline'}
                    size="small"
                    color={participant.isConnected ? 'success' : 'default'}
                  />
                </ListItem>
              )) || (
                <ListItem>
                  <ListItemText
                    secondary="No participants"
                  />
                </ListItem>
              )}
            </List>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};
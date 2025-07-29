import React, { useEffect, useState } from 'react';
import {
  Box,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
  IconButton,
  LinearProgress,
  Chip,
  useTheme,
} from '@mui/material';
import {
  Computer as ComputerIcon,
  People as PeopleIcon,
  Timer as TimerIcon,
  Security as SecurityIcon,
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import { useApi } from '../hooks/useApi';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface DashboardStats {
  totalSessions: number;
  activeSessions: number;
  totalDevices: number;
  onlineDevices: number;
  totalUsers: number;
  activeUsers: number;
  averageSessionDuration: number;
  sessionTrend: number;
}

interface StatCardProps {
  title: string;
  value: number | string;
  icon: React.ReactNode;
  color: string;
  trend?: number;
  loading?: boolean;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, trend, loading }) => {
  const theme = useTheme();
  
  return (
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="textSecondary" gutterBottom variant="body2">
              {title}
            </Typography>
            <Typography variant="h4" component="div">
              {loading ? '-' : value}
            </Typography>
            {trend !== undefined && (
              <Box display="flex" alignItems="center" mt={1}>
                {trend > 0 ? (
                  <TrendingUpIcon color="success" fontSize="small" />
                ) : (
                  <TrendingDownIcon color="error" fontSize="small" />
                )}
                <Typography
                  variant="body2"
                  color={trend > 0 ? 'success.main' : 'error.main'}
                  sx={{ ml: 0.5 }}
                >
                  {Math.abs(trend)}%
                </Typography>
              </Box>
            )}
          </Box>
          <Box
            sx={{
              p: 2,
              borderRadius: '50%',
              backgroundColor: `${color}.100`,
              color: `${color}.main`,
            }}
          >
            {icon}
          </Box>
        </Box>
        {loading && <LinearProgress sx={{ mt: 2 }} />}
      </CardContent>
    </Card>
  );
};

export const Dashboard: React.FC = () => {
  const theme = useTheme();
  const { get, loading } = useApi();
  const [stats, setStats] = useState<DashboardStats>({
    totalSessions: 0,
    activeSessions: 0,
    totalDevices: 0,
    onlineDevices: 0,
    totalUsers: 0,
    activeUsers: 0,
    averageSessionDuration: 0,
    sessionTrend: 0,
  });
  const [refreshing, setRefreshing] = useState(false);

  const fetchDashboardData = async () => {
    try {
      setRefreshing(true);
      const response = await get<DashboardStats>('/api/dashboard/stats');
      setStats(response);
    } catch (error) {
      console.error('Failed to fetch dashboard stats:', error);
    } finally {
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const chartData = {
    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    datasets: [
      {
        label: 'Sessions',
        data: [65, 59, 80, 81, 96, 85, 90],
        fill: false,
        borderColor: theme.palette.primary.main,
        tension: 0.1,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    plugins: {
      legend: {
        position: 'top' as const,
      },
      title: {
        display: true,
        text: 'Weekly Session Activity',
      },
    },
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Dashboard
        </Typography>
        <IconButton onClick={fetchDashboardData} disabled={refreshing}>
          <RefreshIcon />
        </IconButton>
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Active Sessions"
            value={`${stats.activeSessions} / ${stats.totalSessions}`}
            icon={<ComputerIcon />}
            color="primary"
            trend={stats.sessionTrend}
            loading={loading || refreshing}
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Online Devices"
            value={`${stats.onlineDevices} / ${stats.totalDevices}`}
            icon={<ComputerIcon />}
            color="success"
            loading={loading || refreshing}
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Active Users"
            value={`${stats.activeUsers} / ${stats.totalUsers}`}
            icon={<PeopleIcon />}
            color="info"
            loading={loading || refreshing}
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Avg. Session Time"
            value={`${Math.round(stats.averageSessionDuration)} min`}
            icon={<TimerIcon />}
            color="warning"
            loading={loading || refreshing}
          />
        </Grid>

        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Line data={chartData} options={chartOptions} />
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Recent Activity
            </Typography>
            <Box sx={{ mt: 2 }}>
              {[
                { user: 'John Doe', action: 'Started session', time: '5 min ago', type: 'success' },
                { user: 'Jane Smith', action: 'Ended session', time: '15 min ago', type: 'default' },
                { user: 'Admin', action: 'Added new device', time: '1 hour ago', type: 'info' },
                { user: 'System', action: 'Security scan completed', time: '2 hours ago', type: 'success' },
              ].map((activity, index) => (
                <Box key={index} sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                  <SecurityIcon color="action" sx={{ mr: 2 }} />
                  <Box sx={{ flexGrow: 1 }}>
                    <Typography variant="body2">
                      <strong>{activity.user}</strong> {activity.action}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {activity.time}
                    </Typography>
                  </Box>
                  <Chip
                    label={activity.type}
                    size="small"
                    color={activity.type as any}
                    variant="outlined"
                  />
                </Box>
              ))}
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};
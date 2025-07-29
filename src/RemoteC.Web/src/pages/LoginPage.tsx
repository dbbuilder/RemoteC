import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  CircularProgress,
  Container,
  Stack,
} from '@mui/material';
import { Microsoft as MicrosoftIcon } from '@mui/icons-material';
import Logo from '../assets/logo.svg';

interface LoginPageProps {
  onLogin: () => void;
  isLoading: boolean;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onLogin, isLoading }) => {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}
    >
      <Container maxWidth="sm">
        <Paper
          elevation={24}
          sx={{
            p: 6,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            borderRadius: 2,
          }}
        >
          <Box
            sx={{
              width: 80,
              height: 80,
              mb: 3,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              borderRadius: '50%',
              bgcolor: 'primary.main',
              color: 'white',
              fontSize: '2rem',
              fontWeight: 'bold',
            }}
          >
            RC
          </Box>
          
          <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
            Welcome to RemoteC
          </Typography>
          
          <Typography variant="body1" color="text.secondary" align="center" sx={{ mb: 4 }}>
            Enterprise-grade remote control solution with RustDesk-level performance
          </Typography>
          
          <Stack spacing={3} sx={{ width: '100%', maxWidth: 360 }}>
            <Typography variant="body2" color="text.secondary" align="center">
              Sign in with your organization account to continue
            </Typography>
            
            <Button
              variant="contained"
              size="large"
              fullWidth
              onClick={onLogin}
              disabled={isLoading}
              startIcon={isLoading ? <CircularProgress size={20} /> : <MicrosoftIcon />}
              sx={{
                py: 1.5,
                textTransform: 'none',
                fontSize: '1rem',
              }}
            >
              {isLoading ? 'Signing in...' : 'Sign in with Microsoft'}
            </Button>
            
            <Typography variant="caption" color="text.secondary" align="center">
              By signing in, you agree to our Terms of Service and Privacy Policy
            </Typography>
          </Stack>
          
          <Box sx={{ mt: 4, textAlign: 'center' }}>
            <Typography variant="caption" color="text.secondary">
              Need help? Contact your IT administrator
            </Typography>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
};
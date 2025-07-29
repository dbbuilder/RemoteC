import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Box, Typography, Button, Paper } from '@mui/material';
import { Error as ErrorIcon } from '@mui/icons-material';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error:', error, errorInfo);
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
    window.location.href = '/';
  };

  public render() {
    if (this.state.hasError) {
      return (
        <Box
          display="flex"
          alignItems="center"
          justifyContent="center"
          minHeight="100vh"
          bgcolor="background.default"
        >
          <Paper
            elevation={3}
            sx={{
              p: 4,
              maxWidth: 500,
              textAlign: 'center',
            }}
          >
            <ErrorIcon color="error" sx={{ fontSize: 64, mb: 2 }} />
            <Typography variant="h5" gutterBottom>
              Oops! Something went wrong
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              We're sorry, but something unexpected happened. Please try refreshing the page or
              contact support if the problem persists.
            </Typography>
            {this.state.error && (
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{
                  mt: 2,
                  p: 2,
                  bgcolor: 'grey.100',
                  borderRadius: 1,
                  fontFamily: 'monospace',
                  fontSize: '0.875rem',
                  wordBreak: 'break-word',
                }}
              >
                {this.state.error.message}
              </Typography>
            )}
            <Button
              variant="contained"
              color="primary"
              onClick={this.handleReset}
              sx={{ mt: 3 }}
            >
              Return to Dashboard
            </Button>
          </Paper>
        </Box>
      );
    }

    return this.props.children;
  }
}
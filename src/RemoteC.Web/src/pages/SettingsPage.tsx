import React from 'react';
import { Box, Typography, Alert } from '@mui/material';

export const SettingsPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Settings
      </Typography>
      <Alert severity="info">
        Settings interface coming soon...
      </Alert>
    </Box>
  );
};
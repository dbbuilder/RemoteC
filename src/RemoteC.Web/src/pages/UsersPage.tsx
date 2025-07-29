import React from 'react';
import { Box, Typography, Alert } from '@mui/material';

export const UsersPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Users
      </Typography>
      <Alert severity="info">
        User management interface coming soon...
      </Alert>
    </Box>
  );
};
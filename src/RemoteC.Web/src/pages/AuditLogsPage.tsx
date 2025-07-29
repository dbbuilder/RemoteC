import React from 'react';
import { Box, Typography, Alert } from '@mui/material';

export const AuditLogsPage: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Audit Logs
      </Typography>
      <Alert severity="info">
        Audit logs interface coming soon...
      </Alert>
    </Box>
  );
};
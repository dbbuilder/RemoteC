import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useMsal } from '@azure/msal-react';
import { appConfig } from '../config/appConfig';

interface SignalRContextType {
  connection: HubConnection | null;
  isConnected: boolean;
  sendMessage: (method: string, ...args: any[]) => Promise<void>;
  on: (method: string, callback: (...args: any[]) => void) => void;
  off: (method: string, callback: (...args: any[]) => void) => void;
}

const SignalRContext = createContext<SignalRContextType>({
  connection: null,
  isConnected: false,
  sendMessage: async () => {},
  on: () => {},
  off: () => {},
});

export const useSignalR = () => useContext(SignalRContext);

interface SignalRProviderProps {
  children: React.ReactNode;
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const { accounts, instance } = useMsal();
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const initializeConnection = async () => {
      if (accounts.length === 0) return;

      try {
        // Get access token
        const tokenResponse = await instance.acquireTokenSilent({
          scopes: ['api://remotec/access'],
          account: accounts[0],
        });

        // Build connection
        const newConnection = new HubConnectionBuilder()
          .withUrl(`${appConfig.apiUrl}/hubs/session`, {
            accessTokenFactory: () => tokenResponse.accessToken,
          })
          .withAutomaticReconnect()
          .configureLogging(LogLevel.Information)
          .build();

        // Set up event handlers
        newConnection.onreconnecting(() => {
          console.log('SignalR reconnecting...');
          setIsConnected(false);
        });

        newConnection.onreconnected(() => {
          console.log('SignalR reconnected');
          setIsConnected(true);
        });

        newConnection.onclose(() => {
          console.log('SignalR connection closed');
          setIsConnected(false);
        });

        // Start connection
        await newConnection.start();
        console.log('SignalR connected');
        setConnection(newConnection);
        setIsConnected(true);
      } catch (error) {
        console.error('SignalR connection failed:', error);
      }
    };

    initializeConnection();

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, [accounts, instance]);

  const sendMessage = useCallback(
    async (method: string, ...args: any[]) => {
      if (connection && isConnected) {
        try {
          await connection.invoke(method, ...args);
        } catch (error) {
          console.error(`Failed to invoke ${method}:`, error);
          throw error;
        }
      } else {
        throw new Error('SignalR connection is not established');
      }
    },
    [connection, isConnected]
  );

  const on = useCallback(
    (method: string, callback: (...args: any[]) => void) => {
      if (connection) {
        connection.on(method, callback);
      }
    },
    [connection]
  );

  const off = useCallback(
    (method: string, callback: (...args: any[]) => void) => {
      if (connection) {
        connection.off(method, callback);
      }
    },
    [connection]
  );

  const value: SignalRContextType = {
    connection,
    isConnected,
    sendMessage,
    on,
    off,
  };

  return <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>;
};
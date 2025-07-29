//! Reliability layer for transport protocols
//!
//! Provides packet acknowledgment, retransmission, and ordering.

use std::collections::{HashMap, VecDeque};
use std::time::{Duration, Instant};

/// Packet tracking for reliability
#[derive(Debug)]
pub struct ReliabilityLayer {
    /// Next sequence number to use
    next_sequence: u64,
    /// Sent packets awaiting acknowledgment
    sent_packets: HashMap<u64, SentPacket>,
    /// Received packets buffer for reordering
    receive_buffer: HashMap<u64, ReceivedPacket>,
    /// Next expected sequence number
    next_expected: u64,
    /// Maximum retransmission attempts
    max_retries: u32,
    /// Retransmission timeout
    rto: Duration,
}

#[derive(Debug)]
struct SentPacket {
    data: Vec<u8>,
    sent_at: Instant,
    retry_count: u32,
}

#[derive(Debug)]
struct ReceivedPacket {
    data: Vec<u8>,
    received_at: Instant,
}

impl ReliabilityLayer {
    pub fn new(max_retries: u32) -> Self {
        Self {
            next_sequence: 0,
            sent_packets: HashMap::new(),
            receive_buffer: HashMap::new(),
            next_expected: 0,
            max_retries,
            rto: Duration::from_millis(100),
        }
    }
    
    /// Get next sequence number
    pub fn next_sequence(&mut self) -> u64 {
        let seq = self.next_sequence;
        self.next_sequence += 1;
        seq
    }
    
    /// Track sent packet
    pub fn track_sent(&mut self, sequence: u64, data: Vec<u8>) {
        self.sent_packets.insert(sequence, SentPacket {
            data,
            sent_at: Instant::now(),
            retry_count: 0,
        });
    }
    
    /// Process acknowledgment
    pub fn process_ack(&mut self, sequence: u64) -> Option<Duration> {
        if let Some(packet) = self.sent_packets.remove(&sequence) {
            Some(packet.sent_at.elapsed())
        } else {
            None
        }
    }
    
    /// Get packets that need retransmission
    pub fn get_retransmissions(&mut self) -> Vec<(u64, Vec<u8>)> {
        let now = Instant::now();
        let mut retransmissions = Vec::new();
        
        for (seq, packet) in self.sent_packets.iter_mut() {
            if now.duration_since(packet.sent_at) > self.rto && packet.retry_count < self.max_retries {
                packet.retry_count += 1;
                packet.sent_at = now;
                retransmissions.push((*seq, packet.data.clone()));
            }
        }
        
        retransmissions
    }
    
    /// Process received packet
    pub fn process_received(&mut self, sequence: u64, data: Vec<u8>) -> Vec<Vec<u8>> {
        // Store in buffer
        self.receive_buffer.insert(sequence, ReceivedPacket {
            data,
            received_at: Instant::now(),
        });
        
        // Deliver in-order packets
        let mut delivered = Vec::new();
        while let Some(packet) = self.receive_buffer.remove(&self.next_expected) {
            delivered.push(packet.data);
            self.next_expected += 1;
        }
        
        delivered
    }
    
    /// Update retransmission timeout based on RTT
    pub fn update_rto(&mut self, rtt: Duration) {
        // Simple RTO calculation: RTT * 1.5
        self.rto = rtt.mul_f32(1.5);
    }
}
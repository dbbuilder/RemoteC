//! Congestion control for transport protocols
//!
//! Implements congestion control algorithms to optimize throughput and minimize latency.

use std::time::{Duration, Instant};

/// Congestion control algorithm types
#[derive(Debug, Clone, Copy)]
pub enum CongestionAlgorithm {
    /// Basic AIMD (Additive Increase Multiplicative Decrease)
    Aimd,
    /// BBR (Bottleneck Bandwidth and Round-trip propagation time)
    Bbr,
    /// CUBIC (default for many systems)
    Cubic,
}

/// Congestion controller
#[derive(Debug)]
pub struct CongestionController {
    algorithm: CongestionAlgorithm,
    /// Current congestion window (in packets)
    cwnd: f64,
    /// Slow start threshold
    ssthresh: f64,
    /// Current RTT
    rtt: Duration,
    /// Minimum RTT observed
    min_rtt: Duration,
    /// Bandwidth estimate (bytes per second)
    bandwidth: f64,
    /// Last congestion event
    last_congestion: Option<Instant>,
    /// In slow start phase
    slow_start: bool,
}

impl CongestionController {
    pub fn new(algorithm: CongestionAlgorithm) -> Self {
        Self {
            algorithm,
            cwnd: 10.0, // Initial window
            ssthresh: f64::MAX,
            rtt: Duration::from_millis(100),
            min_rtt: Duration::from_secs(1),
            bandwidth: 0.0,
            slow_start: true,
            last_congestion: None,
        }
    }
    
    /// Update RTT measurement
    pub fn update_rtt(&mut self, rtt: Duration) {
        self.rtt = rtt;
        if rtt < self.min_rtt {
            self.min_rtt = rtt;
        }
    }
    
    /// Process acknowledgment
    pub fn on_ack(&mut self, acked_bytes: usize) {
        match self.algorithm {
            CongestionAlgorithm::Aimd => self.aimd_on_ack(acked_bytes),
            CongestionAlgorithm::Bbr => self.bbr_on_ack(acked_bytes),
            CongestionAlgorithm::Cubic => self.cubic_on_ack(acked_bytes),
        }
    }
    
    /// Process packet loss
    pub fn on_loss(&mut self) {
        match self.algorithm {
            CongestionAlgorithm::Aimd => self.aimd_on_loss(),
            CongestionAlgorithm::Bbr => self.bbr_on_loss(),
            CongestionAlgorithm::Cubic => self.cubic_on_loss(),
        }
        
        self.last_congestion = Some(Instant::now());
        self.slow_start = false;
    }
    
    /// Get current congestion window
    pub fn cwnd(&self) -> usize {
        self.cwnd as usize
    }
    
    /// Check if we can send more data
    pub fn can_send(&self, in_flight: usize) -> bool {
        in_flight < self.cwnd as usize
    }
    
    /// Get pacing rate (bytes per second)
    pub fn pacing_rate(&self) -> f64 {
        if self.bandwidth > 0.0 {
            self.bandwidth * 1.25 // 25% headroom
        } else {
            // Estimate based on cwnd and RTT
            (self.cwnd * 1500.0) / self.rtt.as_secs_f64()
        }
    }
    
    // AIMD implementation
    fn aimd_on_ack(&mut self, _acked_bytes: usize) {
        if self.slow_start {
            self.cwnd += 1.0;
            if self.cwnd >= self.ssthresh {
                self.slow_start = false;
            }
        } else {
            self.cwnd += 1.0 / self.cwnd; // Additive increase
        }
    }
    
    fn aimd_on_loss(&mut self) {
        self.ssthresh = self.cwnd / 2.0;
        self.cwnd = self.ssthresh; // Multiplicative decrease
    }
    
    // Simplified BBR implementation
    fn bbr_on_ack(&mut self, acked_bytes: usize) {
        // Update bandwidth estimate
        let delivery_rate = acked_bytes as f64 / self.rtt.as_secs_f64();
        self.bandwidth = self.bandwidth.max(delivery_rate);
        
        // Set cwnd based on BDP (Bandwidth-Delay Product)
        let bdp = self.bandwidth * self.min_rtt.as_secs_f64();
        self.cwnd = (bdp / 1500.0).max(10.0); // Assuming 1500 byte packets
    }
    
    fn bbr_on_loss(&mut self) {
        // BBR doesn't reduce cwnd on loss
        // It relies on bandwidth and RTT measurements
    }
    
    // Simplified CUBIC implementation
    fn cubic_on_ack(&mut self, _acked_bytes: usize) {
        if self.slow_start {
            self.cwnd += 1.0;
            if self.cwnd >= self.ssthresh {
                self.slow_start = false;
            }
        } else {
            // Simplified cubic function
            let t = Instant::now()
                .duration_since(self.last_congestion.unwrap_or(Instant::now() - Duration::from_secs(1)))
                .as_secs_f64();
            
            let k = (self.ssthresh * 0.8).cbrt();
            let w_cubic = 0.4 * (t - k).powi(3) + self.ssthresh;
            
            if w_cubic > self.cwnd {
                self.cwnd = w_cubic;
            } else {
                self.cwnd += 1.0 / self.cwnd;
            }
        }
    }
    
    fn cubic_on_loss(&mut self) {
        self.ssthresh = self.cwnd * 0.8;
        self.cwnd = self.ssthresh;
    }
}
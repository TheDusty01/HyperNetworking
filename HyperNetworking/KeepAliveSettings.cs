using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking
{
    public struct KeepAliveSettings
    {
        private TimeSpan interval;
        private TimeSpan timeoutTime;

        public KeepAliveSettings(bool enabled = true, TimeSpan interval = default, TimeSpan timeoutTime = default)
        {
            this.interval = interval == default ? TimeSpan.FromMilliseconds(15000) : interval;
            this.timeoutTime = timeoutTime == default ? TimeSpan.FromMilliseconds(30000) : timeoutTime;
        }

        /// <summary>
        /// If set to true, then the TCP keep-alive option on a TCP connection will be enabled using the specified <see cref="KeepAliveTime"/> and <see cref="KeepAliveInterval"/> values.
        /// If set to false, then the TCP keep-alive option is disabled and the remaining parameters are ignored.
        /// The default value is <see langword="true"/>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Specifies the interval, in milliseconds, between when successive keep-alive packets are sent if no acknowledgement is received.
        /// The value must be greater than <see langword="0"/>. If a value of less than or equal to zero is passed an <see cref="ArgumentOutOfRangeException"/> is thrown.
        /// The default value is <see langword="15000"/>ms.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                return interval;
            }
            set
            {
                if (value.TotalMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(Interval), "Must be greater than zero.");
                interval = value;
            }
        }

        /// <summary>
        /// Specifies the time until a timeout is recieved.
        /// The value must be greater than <see langword="0"/>. If a value of less than or equal to zero is passed an <see cref="ArgumentOutOfRangeException"/> is thrown.
        /// The default value is <see langword="5"/>.
        /// </summary>
        public TimeSpan TimeoutTime
        {
            get
            {
                return timeoutTime;
            }
            set
            {
                if (value.TotalMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(TimeoutTime), "Must be greater than zero.");
                timeoutTime = value;
            }
        }


    }
}

﻿//======= Copyright (c) Melbourne Robotics Lab, All rights reserved. ===============
using UnityEngine;

namespace VRProEP.ProsthesisCore
{
    /// <summary>
    /// Virtual encoder to provide angular position and velocity for a Unity HingeJoint.
    /// </summary>
    public class VirtualEncoderManager : SensorManager
    {
        private HingeJoint virtualJoint;

        public enum VirtualEncoderChannels
        {
            ANG_POS,
            ANG_VEL
        }

        /// <summary>
        /// Virtual encoder to provide angular position and velocity for a Unity HingeJoint.
        /// </summary>
        public VirtualEncoderManager() : base(2, SensorType.VirtualEncoder)
        {
        }

        /// <summary>
        /// Virtual encoder to provide angular position and velocity for a Unity HingeJoint.
        /// </summary>
        /// <param name="virtualJoint">The HingeJoint to assign to the sensor.</param>
        public VirtualEncoderManager(HingeJoint virtualJoint) : base(2, SensorType.VirtualEncoder)
        {
            SetVirtualJoint(virtualJoint);
        }

        /// <summary>
        /// Assigns the given HingeJoint to the sensor.
        /// </summary>
        /// <param name="virtualJoint">The HingeJoint to assign to the sensor.</param>
        public void SetVirtualJoint(HingeJoint virtualJoint)
        {
            if (virtualJoint == null)
                throw new System.ArgumentNullException();

            this.virtualJoint = virtualJoint;
        }

        /// <summary>
        /// Returns raw tracking information for the selected channel.
        /// 0: Angular displacement given in degrees.
        /// 1: Angular velocity given in degrees per second.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <returns>Raw tracking data for the given channel.</returns>
        public override float GetRawData(int channel)
        {
            if (channel >= ChannelSize)
                throw new System.ArgumentOutOfRangeException("The requested channel number is greater than the available number of channels.");
            else if (channel < 0)
                throw new System.ArgumentOutOfRangeException("The channel number starts from 0.");

            return GetAllRawData()[channel];

        }

        /// <summary>
        /// Returns raw tracking information for the selected channel identifier.
        /// ANG_POS: Angular displacement given in degrees.
        /// ANG_VEL: Angular velocity given in degrees per second.
        /// </summary>
        /// <param name="channel">The channel/data identifier.</param>
        /// <returns>Raw tracking data for the given channel.</returns>
        public override float GetRawData(string channel)
        {
            int channelNum = (int)System.Enum.Parse(typeof(VirtualEncoderChannels), channel);

            return GetRawData(channelNum);
        }

        /// <summary>
        /// Returns all raw joint data in an array.
        /// Angular displacement given in degrees.
        /// Angular velocity given in degrees per second.
        /// </summary>
        /// <returns>The array with raw angular position and velocity data.</returns>
        public override float[] GetAllRawData()
        {
            // Get current prosthesis angle
            float[] x = new float[2]
            {
            virtualJoint.angle,
            virtualJoint.velocity
            };
            return x;
        }

        /// <summary>
        /// Gives data in radians instead of degrees.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <returns>Pre-processed sensor data for the given channel.</returns>
        public override float GetProcessedData(int channel)
        {
            if (channel >= ChannelSize)
                throw new System.ArgumentOutOfRangeException("The requested channel number is greater than the available number of channels.");
            else if (channel < 0)
                throw new System.ArgumentOutOfRangeException("The channel number starts from 0.");

            return GetAllProcessedData()[channel];
        }

        /// <summary>
        /// Returns pre-process tracking information for the selected channel identifier.
        /// ANG_POS: Angular displacement given in radians.
        /// ANG_VEL: Angular velocity given in radians per second.
        /// </summary>
        /// <param name="channel">The channel/data identifier.</param>
        /// <returns>Pre-processed sensor data for the given channel.</returns>
        public override float GetProcessedData(string channel)
        {
            int channelNum = (int)System.Enum.Parse(typeof(VirtualEncoderChannels), channel);

            return GetProcessedData(channelNum);
        }

        /// <summary>
        /// Returns all pre-processed joint data in an array.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <returns>The array with pre-processed angular position and velocity data.</returns>
        public override float[] GetAllProcessedData()
        {
            // Get current prosthesis angle
            float[] x = new float[2]
            {
            virtualJoint.angle * (Mathf.PI / 180.0f),
            virtualJoint.velocity * (Mathf.PI / 180.0f)
            };
            return x;
        }

        /// <summary>
        /// Updates the configuration of a parameter defined by the "command" parameter to the provided "value".
        /// Not implemented
        /// </summary>
        /// <remarks>Commands are defined by the implementing class.</remarks>
        /// <param name="command">The configuration command as established by the implementing class.</param>
        /// <param name="value">The value to update the configuration parameter determined by "command".</param>
        public override void Configure(string command, dynamic value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Updates the configuration of a parameter defined by the "command" parameter to the provided "value".
        /// </summary>
        /// <remarks>Commands are defined by the implementing class.</remarks>
        /// <param name="command">The configuration command as established by the implementing class.</param>
        /// <param name="value">The value to update the configuration parameter determined by "command".</param>
        public override void Configure(string command, string value)
        {
            throw new System.NotImplementedException();
        }
    }
}

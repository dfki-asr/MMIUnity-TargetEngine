// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using MMICoSimulation;
using MMICSharp.Access;
using MMICSharp.Common;
using MMICSharp.MMICSharp_Core.MMICore.Common.Tools;
using MMIStandard;
using MMIUnity.TargetEngine.Scene;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMIUnity.TargetEngine
{
    /// <summary>
    /// Wrapper class to a remote cosimulation
    /// </summary>
    public class RemoteCoSimulation : MMICoSimulator
    {

        public override event EventHandler<MSimulationEvent> MSimulationEventHandler;

        #region private variables


        private readonly IMotionModelUnitAccess remoteCoSimulationMMU;

        /// <summary>
        /// The referenced avatar
        /// </summary>
        private readonly MMIAvatar avatar;


        /// <summary>
        /// The service access 
        /// </summary>
        private readonly IServiceAccess serviceAccess;

        #endregion
        
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="mmus"></param>
        /// <param name="avatar"></param>
        public RemoteCoSimulation(IMotionModelUnitAccess coSimulationMMU, IServiceAccess serviceAccess, MMIAvatar avatar, Dictionary<string, string> priorities) : base(new List<IMotionModelUnitAccess>() { coSimulationMMU })
        {
            this.avatar = avatar;
            this.serviceAccess = serviceAccess;

            this.remoteCoSimulationMMU = coSimulationMMU;
            timeProfiler = TimeProfiler.GetProfiler("RemoteCoSimulationLog", "CoSimulation");
        }


        /// <summary>
        /// Co Simulator has to be initialized before usage
        /// </summary>
        /// <param name="avatarDescription"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public override MBoolResponse Initialize(MAvatarDescription avatarDescription, Dictionary<string, string> properties)
        {
            MBoolResponse result = null;
            timeProfiler.WatchCodeSnippet("RemoteCoSimulation_Initialize",
                () => result = this.remoteCoSimulationMMU.Initialize(avatarDescription, properties),
                FrameNumber);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="avatarState"></param>
        public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState avatarState)
        {
            MBoolResponse result = null;
            timeProfiler.WatchCodeSnippet("RemoteCoSimulation_AssignInstruction_" + instruction.ID,
                () => result = this.remoteCoSimulationMMU.AssignInstruction(instruction, avatarState),
                FrameNumber);
            return result;
        }

        public override MBoolResponse Abort(string instructionId)
        {
            return this.remoteCoSimulationMMU.Abort(instructionId);
        }

        public override MSimulationResult DoStep(double time, MSimulationState avatarState)
        {
            var stopwatchComplete = timeProfiler.StartWatch();
            var stopwatch = timeProfiler.StartWatch();
            //Call the remote cosimulation
            MSimulationResult result = this.remoteCoSimulationMMU.DoStep(time, avatarState);
            timeProfiler.StopWatch("RemoteCoSimulation_DoStep", stopwatch, FrameNumber);

            //Fire events
            if (result != null && result.Events != null && result.Events.Count > 0)
            {
                foreach (MSimulationEvent simEvent in result.Events)
                {
                    this.MSimulationEventHandler?.Invoke(this, simEvent);
                }
            }

            try
            {
                this.avatar.AssignPostureValues(result.Posture);

            }
            catch (Exception)
            {
                Debug.LogError("Problem assigning posture using remote co-simulation");
            }

            timeProfiler.StopWatch("RemoteCoSimulation_Complete_DoStep", stopwatchComplete, FrameNumber);

            return result;
        }

        public override byte[] CreateCheckpoint()
        {
            return this.remoteCoSimulationMMU.CreateCheckpoint();
        }



        public override MBoolResponse RestoreCheckpoint(byte[] data)
        {
            return this.remoteCoSimulationMMU.RestoreCheckpoint(data);
        }

        public override Dictionary<string, string> ExecuteFunction(string name, Dictionary<string, string> parameters)
        {
            return this.remoteCoSimulationMMU.ExecuteFunction(name, parameters);
        }

    }
}

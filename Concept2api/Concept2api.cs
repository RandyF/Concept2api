using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Diagnostics;
using AJR_Utils;

using CSAFE_Fitness;

namespace Concept2api
{
    public enum C2TimeDistance { TimeSeconds, DistanceMeters }

    public enum C2ServerMessage : byte
    {
        GET_CONFIG_DATA = 0x00,
        GET_MACHINE_STATUS = 0x01,
        GET_WORKOUT_DATA = 0x10,
        LOAD_DISTANCE_WORKOUT = 0xA0,
        START_WORKOUT = 0xB0
    }

    public class PerformanceMonitor : CSAFE_Interface
    {
        private AJRUtils _Utils = new AJRUtils();

        private const int _C2VendorID = 0x17A4;

        public CSAFE_ConfigurationData C2_Data(int DeviceID)
        {
            return _Session.Devices[DeviceID].ConfigData.Data;
        }

        //=========================================================================================================================
        // Constructors
        //=========================================================================================================================
        public PerformanceMonitor() { }

        public PerformanceMonitor(bool AutoConnect)
        {
            Discover_PMs();
        }

        //=========================================================================================================================
        // Rescan for PerformanceMonitors
        //=========================================================================================================================
        public int Discover_PMs()
        {
            _Utils.warning(String.Format("Scanning for Performance Monitors..."));

            int DeviceCount = RequeryVendorDeviceList(_C2VendorID);

            _Utils.warning(String.Format("Found {0} PMs.", DeviceCount));
            return DeviceCount;
        }

        public void ExecuteCommands(int DeviceID)
        {
            ExecuteCommandBuffer(DeviceID, 0x02);

        }

        //=========================================================================================================================
        // COMMANDS
        //=========================================================================================================================

        //-------------------------------------------------------------------------------------------------------------------------
        // Reset Environment
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_EnvironmentReset(int DeviceID)
        {
            bool tRet = CSAFE_CtrlCmds.cmdReset();
            tRet &= CSAFE_CtrlCmds.cmdGoReady();

            ExecuteCommands(DeviceID);

            _Session.Devices[DeviceID].ConfigData = new CSAFE_ConfigurationDataManager(ref _Session);

            return tRet;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Get Machine Status
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_GetMachineStatus(int DeviceID)
        {
            bool tRet = CSAFE_CtrlCmds.cmdGetStatus();
            tRet &= CSAFE_ReqData.cmdGetErrorCode();
            tRet &= C2_GetErrorValue();

            ExecuteCommands(DeviceID);

            return tRet;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // Get Monitor Information
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_GetMonitorData(int DeviceID)
        {
            bool tRet = CSAFE_ReqWorkoutData.cmdGetTWork();
            tRet &= CSAFE_ReqWorkoutData.cmdGetHorizontal();
            tRet &= CSAFE_ReqWorkoutData.cmdGetPace();
            tRet &= CSAFE_ReqWorkoutData.cmdGetPower();
            tRet &= CSAFE_ReqWorkoutData.cmdGetCalories();
            tRet &= CSAFE_ReqWorkoutData.cmdGetCadence();
            tRet &= CSAFE_ReqWorkoutData.cmdGetHRCur();

            ExecuteCommands(DeviceID);

            return tRet;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Get Error Value
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_GetErrorValue()
        {
            byte[] cmdData = new byte[] { 0xC9 };

            Console.WriteLine("[C2] Pushing C2_GetErrorValue.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Set Horizontal distance goal
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_SetHorizontalDistanceGoal(int HorizDistMeters)
        {
            return CSAFE_ConfigWorkoutCmds.cmdSetHorizontal(HorizDistMeters, 36);
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Set Power goal
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_SetPowerGoal(int Watts)
        {
            return CSAFE_ConfigWorkoutCmds.cmdSetPower(Watts, 88);
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Set Program
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_SetProgram(byte Program)
        {
            return CSAFE_ConfigWorkoutCmds.cmdSetProgram((int)Program);
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Set Split Duration
        //-------------------------------------------------------------------------------------------------------------------------
        public bool C2_SetSplitDuration(C2TimeDistance TimeOrDistance, int Duration )
        {
            byte[] cmdData = new byte[] { 0x1A, 0x07, 0x05, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            switch (TimeOrDistance)
            {
                case C2TimeDistance.TimeSeconds:
                    cmdData[4] = 0x00;
                    break;
                case C2TimeDistance.DistanceMeters:
                    cmdData[4] = 0x80;
                    break;
            }

            byte[] valBytes = BitConverter.GetBytes(Duration);
            cmdData[5] = valBytes[0];
            cmdData[6] = valBytes[1];
            cmdData[7] = valBytes[2];
            cmdData[8] = valBytes[3];

            Console.WriteLine("[C2] Pushing C2_SetSplitDuration.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

    }
}

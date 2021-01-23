using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using CSAFE_Fitness;
using Concept2api;

using System.Threading;




namespace AJRTesting
{
    class UnitySandbox
    {

        static bool SandboxInterrupted = true;
        static PerformanceMonitor _PM = new PerformanceMonitor(true);

        public void Init()
        {
            //int cntStreams = _PM.OpenStream(0);
            //Console.WriteLine("Opened {0} Streams", cntStreams);

            // RESET EVERYTHING
            //_PM.C2_EnvironmentReset(0);
            //Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());

            // GET MACHINE STATUS
            if (_PM.C2_GetMachineStatus(0)) Console.WriteLine("Executed Command C2_GetMachineStatus");
            Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());

            // CREATE A WORKOUT
            if (_PM.C2_SetHorizontalDistanceGoal(100)) Console.WriteLine("Added Command C2_SetHorizontalDistanceGoal");
            if (_PM.C2_SetSplitDuration(C2TimeDistance.DistanceMeters, 100)) Console.WriteLine("Added Command C2_SetSplitDuration");
            if (_PM.C2_SetPowerGoal(100)) Console.WriteLine("Added Command C2_SetPowerGoal");
            if (_PM.C2_SetProgram(0)) Console.WriteLine("Added Command C2_SetProgram");
            if (_PM.CSAFE_CtrlCmds.cmdGoIdle()) Console.WriteLine("Added Command cmdGoIdle");
            _PM.ExecuteCommands(0);
            Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());

            // START A WORKOUT
            if (_PM.CSAFE_CtrlCmds.cmdGoInUse()) Console.WriteLine("Added Command cmdGoInUse");
            _PM.ExecuteCommands(0);
            Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());

            c2distances = new System.IO.StreamWriter(@"C:\temp\c2dist.csv", false);
            c2distances.WriteLine("Target, Actual, _lastZ, _VelocityZ, tDeltaT, tDeltaD, updatedV");

                ThreadStart threadUnityFrameStart = new ThreadStart(UnityFrame);
            Console.WriteLine("In Main: Creating the Unity thread");
            Thread threadUnity = new Thread(threadUnityFrameStart);


            ThreadStart threadPollingStart = new ThreadStart(PollWorkoutData);
            Console.WriteLine("In Main: Creating the Child thread");
            Thread threadPolling = new Thread(threadPollingStart);


            SandboxInterrupted = false;

            threadUnity.Start();
            threadPolling.Start();
            Console.WriteLine("Press Any Key to Quit");
            Console.ReadKey();
            SandboxInterrupted = true;
            Console.ReadKey();
            Console.WriteLine("Sandbox Complete.");
        }

        private int _WorkoutDistanceMeters = 0;

        private void PollWorkoutData()
        {
            Console.WriteLine("Polling Workout Data");
            Console.WriteLine(_PM.Session.Devices[0].ConfigData.Data.SlaveState);
            while (!SandboxInterrupted && (_PM.Session.Devices[0].ConfigData.Data.SlaveState == MachineState.InUse))
            {
                _PM.C2_GetMonitorData(0);
                //Console.WriteLine(_PM.C2_Data(0).OneLine());
                _WorkoutDistanceMeters = _PM.Session.Devices[0].ConfigData.Data.WorkoutDistanceMeters;
                Thread.Sleep(50);
            }
            Console.WriteLine("Polling Done");
        }

        float _posZ = 0.0f;
        float _TargetZ = 0.0f;

        float _VelocityZ = 0.0f;

        float simTime = 0.0f;

        float deltaTime_ms = 1.666f;
        private float _lastPollTime;
        private float _lastZ = 0;

        System.IO.StreamWriter c2distances;

        private void UnityFrame()
        {
            Console.WriteLine("Unity Start");
            while (!SandboxInterrupted)
            {


                CalculatePosition();

                //Console.Write("{0} ", simTime);

                Thread.Sleep(TimeSpan.FromMilliseconds(deltaTime_ms));
                simTime = simTime += (deltaTime_ms/1000);
            }
            Console.WriteLine("Unity Done");

        }

        private void CalculatePosition()
        {
            float tDeltaT = 10000;
            float tDeltaD = 0;
            float newZ = _posZ;

            float VelocityAdjustFactor = 1.015f;

            bool updatedV = false;

            _TargetZ = (float)_WorkoutDistanceMeters;
            Console.Write("Z0:{0} Z1:{1} ", _posZ, _TargetZ);
            c2distances.Write("{0,10}, {1,10}", _TargetZ, _posZ);


            if (_lastZ < _TargetZ) // Update Velocity
            {
                tDeltaT = simTime - _lastPollTime;
                tDeltaD = _TargetZ - _lastZ;

                _VelocityZ = tDeltaD / tDeltaT;

                _lastPollTime = simTime;

                _lastZ = _TargetZ;
                updatedV = true;

            }
            else updatedV = false;

            Console.Write("Vz:{0}, dT:{1} dD:{2} ", _VelocityZ, tDeltaT, tDeltaD);
            c2distances.Write(", {0,10}, {1,10}, {2,10}, {3,10}, {4,10}", _lastZ, _VelocityZ, tDeltaT, tDeltaD, updatedV);

            newZ = _posZ + (_VelocityZ * (deltaTime_ms/1000));
            Console.Write("Zn:{0} ", newZ);

            if (newZ < _TargetZ) // Catch Up
            {
                _VelocityZ = _VelocityZ * VelocityAdjustFactor;
            }
            if (newZ > _TargetZ) _VelocityZ = _VelocityZ / VelocityAdjustFactor;

            //if(Math.Abs(_TargetZ - newZ) < CloseLockDistance) newZ = _TargetZ;


            _posZ = newZ;

            Console.WriteLine();
            c2distances.WriteLine();

        }

    }


}

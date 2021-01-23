using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Concept2api;
using CSAFE_Fitness;

namespace Concept2Server
{
    public enum  C2ServerMessage : byte
    {
        GET_CONFIG_DATA = 0x00,
        GET_MACHINE_STATUS = 0x01,
        GET_WORKOUT_DATA = 0x10,
        LOAD_DISTANCE_WORKOUT = 0xA0,
        START_WORKOUT = 0xB0
    }

    class C2ServerHandler
    {
        static PerformanceMonitor _PM = new PerformanceMonitor(true);

        public int DeviceCount { get { return _PM.Session.Devices.Count; } }

        public C2ServerHandler()
        {
        }


        public bool HandleClientRequest(byte[] ClientBytes, out byte[] ReplyMsg)
        {
            Console.WriteLine("[C2ServerHandler] Processing Data");

            Console.WriteLine(DumpHex(ClientBytes));

            

            switch((C2ServerMessage)ClientBytes[0])
            {
                case C2ServerMessage.GET_CONFIG_DATA:
                    Console.WriteLine("[C2ServerHandler] Returning ConfigData Package");
                    ReplyMsg = SerializedConfigData();
                    return true;

                case C2ServerMessage.GET_MACHINE_STATUS:
                    Console.WriteLine("[C2ServerHandler] Getting Machine Status");
                    if (_PM.C2_GetMachineStatus(0)) Console.WriteLine("Executed Command C2_GetMachineStatus");
                    Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());
                    ReplyMsg = SerializedConfigData();
                    return true;


                case C2ServerMessage.GET_WORKOUT_DATA:
                    //Console.WriteLine("[C2ServerHandler] Getting Machine Status");
                    if (_PM.C2_GetMonitorData(0))
                    {
                        //Console.WriteLine("Executed Command C2_GetMonitorData");
                        Console.WriteLine(_PM.Session.Devices[0].ConfigData.OneLine());
                        ReplyMsg = SerializedConfigData();
                        return true;
                    }
                    ReplyMsg = new byte[1] { 0x00 };
                    return false;


                case C2ServerMessage.LOAD_DISTANCE_WORKOUT:

                    Console.WriteLine("[C2ServerHandler] Loading Distance Workout");
                    if (_PM.C2_SetHorizontalDistanceGoal(100)) Console.WriteLine("Added Command C2_SetHorizontalDistanceGoal");
                    if (_PM.C2_SetSplitDuration(C2TimeDistance.DistanceMeters, 100)) Console.WriteLine("Added Command C2_SetSplitDuration");
                    if (_PM.C2_SetPowerGoal(100)) Console.WriteLine("Added Command C2_SetPowerGoal");
                    if (_PM.C2_SetProgram(0)) Console.WriteLine("Added Command C2_SetProgram");
                    if (_PM.CSAFE_CtrlCmds.cmdGoIdle()) Console.WriteLine("Added Command cmdGoIdle");
                    _PM.ExecuteCommands(0);

                    Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());
                    ReplyMsg = SerializedConfigData();
                    return true;

                case C2ServerMessage.START_WORKOUT:
                    Console.WriteLine("[C2ServerHandler] Starting Workout");
                    if (_PM.CSAFE_CtrlCmds.cmdGoInUse()) Console.WriteLine("Added Command cmdGoInUse");
                    _PM.ExecuteCommands(0);
                    Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());
                    ReplyMsg = SerializedConfigData();
                    return true;

                default:
                    ReplyMsg = new byte[1] { 0x00 };
                    return false;


            }



            
        }

        private byte[] SerializedConfigData()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, _PM.Session.Devices[0].ConfigData.Data);
                return stream.ToArray();
            }
        }

        public void DiscoverDevice()
        {
            _PM.Discover_PMs();
        }

        public int OpenStream()
        {
           return _PM.OpenStream(0);
        }

        void ResetPMEnvironment()
        {
            // RESET EVERYTHING
          // _PM.C2_EnvironmentReset(0);
           // Console.WriteLine(_PM.Session.Devices[0].ConfigData.ToString());
        }

        string DumpHex(byte[] buff)
        {
            int iIdx = 0;
            string tStr = String.Format("[{0,03}] ---\n", buff.Length);
            foreach (byte b in buff)
            {
                tStr += String.Format("{0:x02} ", b);

                if (++iIdx % 32 == 0) tStr += "\n";
            }
            tStr += "\n      ---   \n";
            return tStr;
        }

    }
}

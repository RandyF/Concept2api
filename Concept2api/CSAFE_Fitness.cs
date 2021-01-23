using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;


using System.Diagnostics;

using AJR_Utils;
using HidSharp;

namespace CSAFE_Fitness
{
    public enum FrameStatus : byte
    {
        prevOK = 0x00,
        prevReject = 0x01,
        prevBad = 0x02,
        prevNotRdy = 0x03
    }

    public enum MachineState : byte
    {
        Error = 0x00,
        Ready = 0x01,
        Idle = 0x02,
        HaveID = 0x03,
        InUse = 0x05,
        Paused = 0x06,
        Finished = 0x07,
        Manual = 0x08,
        OffLine = 0x09
    }

    //=========================================================================================================================
    // Current Configuration Data
    // Stores the all Previous Received Data
    //=========================================================================================================================
    [Serializable]
    public class CSAFE_ConfigurationData
    {
        //-------------------------------------------------------------------------------------------------------------------------
        // Machine Information
        //-------------------------------------------------------------------------------------------------------------------------
        public bool FrameCount = false;
        public bool StatusReservedFlag = false;
        public MachineState SlaveState = MachineState.OffLine;
        public FrameStatus PrevFrameStatus = FrameStatus.prevNotRdy;

        internal int _ErrorCode = 0x9999;
        public int ErrorCode
        {
            get
            {
                // Saw this in some software I was porting - clears the last error after the read.
                int tErr = _ErrorCode;
                _ErrorCode = 0;
                return tErr;
            }

        }

        public int PM3ExtensionCode = 0;

        //-------------------------------------------------------------------------------------------------------------------------
        // Current Workout Data
        //-------------------------------------------------------------------------------------------------------------------------
        public int WorkoutHours = 0;
        public int WorkoutMinutes = 0;
        public int WorkoutSeconds = 0;
        public int WorkoutTotalSeconds
        {
            get
            {
                return WorkoutHours * 60 * 60
                     + WorkoutMinutes * 60
                     + WorkoutSeconds;
            }
        }
        public string WorkoutDuration
        {
            get { return String.Format("{0,2}:{1,2}:{2,2}", WorkoutHours.ToString("00"), WorkoutMinutes.ToString("00"), WorkoutSeconds.ToString("00")); }
        }

        internal int _WorkoutDistanceRaw = 0;
        internal int _WorkoutDistanceUnits = 0;
        public int WorkoutDistanceMeters
        {
            get
            {
                switch (_WorkoutDistanceUnits)
                {
                    //TODO: more conversions as requred

                    case 0x24: //meters
                    default:
                        return _WorkoutDistanceRaw;
                }

            }
        }

        internal int _PaceRaw = 0;
        internal int _PaceUnits = 0;
        public double PaceSecPer500
        {
            get
            {
                switch (_PaceUnits)
                {
                    //TODO: more conversions as requred

                    case 0x39: //"Kg"  Concept2 uses 2xSeconds/500
                    default:
                        return (double)_PaceRaw / 2.0;

                }

            }
        }

        internal int _PowerRaw = 0;
        internal int _PowerUnits = 0;
        public int PowerWatts
        {
            get
            {
                switch (_PowerUnits)
                {
                    //TODO: more conversions as requred

                    case 0x58: //Watts
                    default:
                        return _PowerRaw;
                }

            }
        }

        public int Calories = 0;

        internal int _CadenceRaw = 0;
        internal int _CadenceUnits = 0;
        public int Cadence
        {
            get
            {
                switch (_CadenceUnits)
                {
                    //TODO: more conversions as requred

                    case 0x54: //SPM
                    default:
                        return _CadenceRaw;
                }

            }
        }

        public int HeartRateBPM = 0;

    }


    public class CSAFE_ConfigurationDataManager
    {
        private AJRUtils _Utils = new AJRUtils();
        private CSAFE_Parse_Response CSAFE_ParseResp;
        private CSAFE_ConfigurationData _Data = new CSAFE_ConfigurationData();
        public CSAFE_ConfigurationData Data { get { return _Data; } }

        //-------------------------------------------------------------------------------------------------------------------------
        // Constructor
        //-------------------------------------------------------------------------------------------------------------------------
        public CSAFE_ConfigurationDataManager(ref CSAFE_Session Session)
        {
            CSAFE_ParseResp = new CSAFE_Parse_Response(ref Session);
        }
        
        //-------------------------------------------------------------------------------------------------------------------------
        // Message Parser
        //-------------------------------------------------------------------------------------------------------------------------
        public void ParseReturnMessage(byte[] ReadBuffer)
        {

            Debug.WriteLine("[CSAFE] Parsing Read Data...");
            Debug.WriteLine(_Utils.DumpHex(ReadBuffer));

            int tPos = 0;

            byte tStatus = BitConverter.GetBytes(ReadBuffer[0])[tPos++];

            _Data.FrameCount = false;
            if ((tStatus & 0b10000000) == 0) _Data.FrameCount = true;

            _Data.StatusReservedFlag = false;
            if ((tStatus & 0b010000000) == 0) _Data.StatusReservedFlag = true;

            _Data.PrevFrameStatus = (FrameStatus)((tStatus & 0b00110000) >> 4);

            _Data.SlaveState = (MachineState)(tStatus & 0b00001111);

            byte[] tBuffer;
            while (tPos < ReadBuffer.Length)
            {
                switch (ReadBuffer[tPos])
                {

                    case 0x9C: // parseErrorCode
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseErrorCode(tBuffer, out Data._ErrorCode);
                        tPos += 5;
                        break;

                    case 0x1A: // parsePM3Extension
                        tBuffer = new byte[2];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 2);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parsePM3Extension(tBuffer, out Data.PM3ExtensionCode);
                        tPos += 2;
                        break;

                    case 0xA0: // parseTWork
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseTWork(tBuffer, out Data.WorkoutHours, out Data.WorkoutMinutes, out Data.WorkoutSeconds);
                        tPos += 5;
                        break;

                    case 0xA1: // parseTWork
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseHorizontal(tBuffer, out Data._WorkoutDistanceRaw, out Data._WorkoutDistanceUnits);
                        tPos += 5;
                        break;

                    case 0xA3: // parseCalories
                        tBuffer = new byte[4];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 4);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseCalories(tBuffer, out Data.Calories);
                        tPos += 4;
                        break;

                    case 0xA6: // parsePace
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parsePace(tBuffer, out Data._PaceRaw, out Data._PaceUnits);
                        tPos += 5;
                        break;

                    case 0xA7: // parseCadence
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseCadence(tBuffer, out Data._CadenceRaw, out Data._CadenceUnits);
                        tPos += 5;
                        break;

                    case 0xB0: // parseHRCur
                        tBuffer = new byte[3];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 3);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parseHRCur(tBuffer, out Data.HeartRateBPM);
                        tPos += 3;
                        break;

                    case 0xB4: // parsePower
                        tBuffer = new byte[5];
                        Array.Copy(ReadBuffer, tPos, tBuffer, 0, 5);  // WHY DO MY INDEX AND RANGES NOT WORK HUH?
                        CSAFE_ParseResp.parsePower(tBuffer, out Data._PowerRaw, out Data._PowerUnits);
                        tPos += 5;
                        break;


                    default:
                        Console.WriteLine("[CSAFE] UNHANDLED Response @ Position {0}: [0x{1:x02}]", tPos, ReadBuffer[tPos]);
                        tPos++;
                        break;


                }

            }

        }

        //-------------------------------------------------------------------------------------------------------------------------
        // Dump String
        //-------------------------------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            string FormatString = $@"  Device Confguration/Data
  ---------------------------------------------------
  |==Status Flags==
  |Frame Flag:            {Data.FrameCount}
  |Slave State:           {Data.SlaveState}
  |Previous Frame Status: {Data.PrevFrameStatus}
  ---------------------------------------------------
  |Error Code             {Data._ErrorCode}
  |PM3 Extension Code     {Data.PM3ExtensionCode}
  ---------------------------------------------------
  |==Workout==
  |Duration:              {Data.WorkoutHours}:{Data.WorkoutMinutes}:{Data.WorkoutSeconds} 
  |Distance (raw):        {Data._WorkoutDistanceRaw}[{Data._WorkoutDistanceUnits}]
  |Pace (raw):            {Data._PaceRaw}[{Data._PaceUnits}]
  |Power (raw):           {Data._PowerRaw}[{Data._PowerUnits}]
  |Calories:              {Data.Calories}
  |Cadence (raw):         {Data._CadenceRaw}[{Data._CadenceUnits}]
  |Heart Rate:            {Data.HeartRateBPM}
  ---------------------------------------------------
";

            return String.Format(FormatString, Data.FrameCount, Data.SlaveState, Data.PrevFrameStatus, Data._ErrorCode, Data.PM3ExtensionCode,
                                Data.WorkoutHours, Data.WorkoutMinutes, Data.WorkoutSeconds, Data._WorkoutDistanceRaw, Data._WorkoutDistanceUnits, 
                                Data._PaceRaw, Data._PaceUnits, Data._PowerRaw, Data._PowerUnits, Data.Calories, Data._CadenceRaw, Data._CadenceUnits, Data.HeartRateBPM);

        }


        //-------------------------------------------------------------------------------------------------------------------------
        // One-Liner String (good for debugging polling)
        //-------------------------------------------------------------------------------------------------------------------------
        public string OneLine()
        {
            return String.Format("[{0} {1}] Dur:{2}   Dist:{3,5}m   Pace:{4,5}s/500m   Pow:{5,3}W   Burn:{6,3}cal   Cad:{7,2}spm   HR:{8,3}bpm", Data.PrevFrameStatus, Data.SlaveState,
                                                      Data.WorkoutDuration, Data.WorkoutDistanceMeters, Data.PaceSecPer500, Data.PowerWatts, Data.Calories, Data.Cadence, Data.HeartRateBPM);
        }



    }
    //=========================================================================================================================


    public class CSAFE_Device
    {
        public HidDevice Device;
        public HidStream Stream;

        public CSAFE_ConfigurationDataManager ConfigData;

        public CSAFE_Device()
        { }

        public CSAFE_Device(ref CSAFE_Session Session, HidDevice NewDevice)
        {
            Debug.WriteLine("[CSAFE] Creating New Device");
            ConfigData = new CSAFE_ConfigurationDataManager(ref Session);
            Device = NewDevice;
            Debug.WriteLine("   DONE!");
        }

        ~CSAFE_Device()
        {
            Debug.WriteLine("[CSAFE] Destroying Device Stream");
            Stream.Flush();
            Stream.Close();
            Stream.Dispose();

            Debug.WriteLine("[CSAFE] Device Destroyed");
        }

    }


    public class CSAFE_Session
    {
        public Dictionary<int, CSAFE_Device> Devices = new Dictionary<int, CSAFE_Device>();
        public CSAFE_Frame TxFrame = new CSAFE_Frame();
    }

    public class CSAFE_Frame
    {
        private AJRUtils _Utils = new AJRUtils();
        private int _MAX_TX_FRAME_LENGTH = 120;

        public byte[] RawData = new byte[0];

        public int RawDataLength
        {
            get { return RawData.Length; }
        }

        public CSAFE_Frame() { }

        public CSAFE_Frame(int MaxLength)
        {
            _MAX_TX_FRAME_LENGTH = MaxLength;
        }

        public void ResetFrame()
        {
            RawData = new byte[0];
        }

        public bool AddCommandBlock(byte[] CmdBlock)
        {
            int tNewLen = RawData.Length + CmdBlock.Length;

            if (tNewLen > _MAX_TX_FRAME_LENGTH)
            {
                Debug.Write("New Array would be too long under the BEST conditions!");
                return false;
            }

            byte[] tNewRaw = new byte[tNewLen];

            Array.Copy(RawData, 0, tNewRaw, 0, RawData.Length);
            Array.Copy(CmdBlock, 0, tNewRaw, RawData.Length, CmdBlock.Length);

            RawData = tNewRaw;

            return true;

        }

        public byte[] GetFormattedTXData(byte Report)
        {
            byte chksum = 0x00;
            uint tLen = 2;
            byte[] tDat = new byte[1024];

            tDat[0] = Report;
            tDat[1] = 0xF1;

            foreach (byte b in RawData)
            {
                //TODO ByteStuff
                chksum ^= b;

                switch (b)
                {
                    case 0xF0:
                        tDat[tLen++] = 0xF3;
                        tDat[tLen++] = 0x00;
                        break;

                    case 0xF1:
                        tDat[tLen++] = 0xF3;
                        tDat[tLen++] = 0x01;
                        break;

                    case 0xF2:
                        tDat[tLen++] = 0xF3;
                        tDat[tLen++] = 0x02;
                        break;

                    case 0xF3:
                        tDat[tLen++] = 0xF3;
                        tDat[tLen++] = 0x03;
                        break;

                    default:
                        tDat[tLen++] = b;
                        break;
                }

            }

            switch (chksum)
            {
                case 0xF0:
                    tDat[tLen++] = 0xF3;
                    tDat[tLen++] = 0x00;
                    break;

                case 0xF1:
                    tDat[tLen++] = 0xF3;
                    tDat[tLen++] = 0x01;
                    break;

                case 0xF2:
                    tDat[tLen++] = 0xF3;
                    tDat[tLen++] = 0x02;
                    break;

                case 0xF3:
                    tDat[tLen++] = 0xF3;
                    tDat[tLen++] = 0x03;
                    break;

                default:
                    tDat[tLen++] = chksum;
                    break;
            }

            tDat[tLen++] = 0xF2;

            byte[] tDatTrimmed = new byte[tLen];
            Array.Copy(tDat, tDatTrimmed, tLen);

            return tDatTrimmed;

        }

        public string DumpTXFrame(byte Report)
        {
            return _Utils.DumpHex(GetFormattedTXData(Report));
        }

        public override string ToString()
        {
            return _Utils.DumpHex(RawData);
        }

    }


    public class CSAFE_Interface
    {
        private AJRUtils _Utils = new AJRUtils();
        internal CSAFE_Session _Session = new CSAFE_Session();
        public CSAFE_Session Session
        {
            get { return _Session; }

        }

        public CSAFE_Control_Commands CSAFE_CtrlCmds;
        public CSAFE_Configuration_Commands CSAFE_ConfigCmds;
        public CSAFE_Configure_Workout_Commands CSAFE_ConfigWorkoutCmds;
        public CSAFE_Request_Data CSAFE_ReqData;
        public CSAFE_Request_Workout_Data CSAFE_ReqWorkoutData;

        public CSAFE_Interface()
        {
            CSAFE_CtrlCmds = new CSAFE_Control_Commands(ref _Session);
            CSAFE_ConfigCmds = new CSAFE_Configuration_Commands(ref _Session);
            CSAFE_ConfigWorkoutCmds = new CSAFE_Configure_Workout_Commands(ref _Session);
            CSAFE_ReqData = new CSAFE_Request_Data(ref _Session);
            CSAFE_ReqWorkoutData = new CSAFE_Request_Workout_Data(ref _Session);
        }

        //=========================================================================================================================
        // Rescan for PerformanceMonitors
        //=========================================================================================================================
        public int RequeryVendorDeviceList(int VendorID)
        {
            Debug.WriteLine("[CSAFE] RequeryVendorDeviceList");
            int tID = 0;

            var usbDeviceList = DeviceList.Local;
            foreach (HidDevice tDev in usbDeviceList.GetHidDevices(VendorID))
            {
                CSAFE_Device tNewDev = new CSAFE_Device(ref _Session, tDev);
                _Session.Devices.Add(tID++, tNewDev);
            }

            Debug.WriteLine("   DONE.");
            return _Session.Devices.Count;
        }

        //=========================================================================================================================
        // Open Streams
        //=========================================================================================================================
        public int OpenStream(int DeviceID)
        {

            if (DeviceID == -1)
            {
                int tCnt = 0;
                foreach (KeyValuePair<int, CSAFE_Device> tDev in _Session.Devices)
                {
                    if (tDev.Value.Device.TryOpen(out tDev.Value.Stream)) tCnt++;
                    return tCnt;
                }
            }
            else
            {
                if (_Session.Devices[DeviceID].Device.TryOpen(out _Session.Devices[DeviceID].Stream)) return 1;
                else return 0;
            }
            return -1;
        }

        //=========================================================================================================================
        // Open Streams
        //=========================================================================================================================
        public int CloseStream(int DeviceID)
        {

            if (DeviceID == -1)
            {
                int tCnt = 0;
                foreach (KeyValuePair<int, CSAFE_Device> tDev in _Session.Devices)
                {
                    tDev.Value.Stream.Close();
                    return tCnt;
                }
            }
            else
            {
                _Session.Devices[DeviceID].Stream.Close();
                return 1;
            }
            return -1;
        }

        //=========================================================================================================================
        // Execute Command In Frame Buffer
        //=========================================================================================================================
        //-------------------------------------------------------------------------------------------------------------------------
        // Main call that allows for one or multiple devices to be addressed.
        //-------------------------------------------------------------------------------------------------------------------------
        public void ExecuteCommandBuffer(int DeviceID, byte Report)
        {
            if (DeviceID == -1)
            {
                foreach (KeyValuePair<int, CSAFE_Device> tDev in _Session.Devices)
                {
                    _ExecuteCommandBuffer(tDev.Key, Report);
                }
            }
            else
                _ExecuteCommandBuffer(DeviceID, Report);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // TXRX a single Device
        //-------------------------------------------------------------------------------------------------------------------------
        private void _ExecuteCommandBuffer(int DeviceID, byte Report)
        {
            Debug.WriteLine("[CSAFE] Executing Command Buffer...");
            HidStream tStream;

            tStream = _Session.Devices[DeviceID].Stream;

            //-------------------------------------------------------------------------------------------------------------------------
            // WRITE COMMAND
            //-------------------------------------------------------------------------------------------------------------------------
            Debug.WriteLine("[CSAFE] WRITE");
            //Console.WriteLine(_Utils.DumpHex(_Session.TxFrame.RawData));
            tStream.Write(_Session.TxFrame.GetFormattedTXData(Report));

            _Session.TxFrame.ResetFrame();


            //-------------------------------------------------------------------------------------------------------------------------
            // READ RESPONSE
            //-------------------------------------------------------------------------------------------------------------------------
            byte[] readdat = new byte[120];
            Debug.WriteLine("[CSAFE] READ");
            tStream.Read(readdat);
            //Console.WriteLine(_Utils.DumpHex(readdat));

            
            //-------------------------------------------------------------------------------------------------------------------------
            // Route Response for Parsing
            //-------------------------------------------------------------------------------------------------------------------------
            byte[] procRead = PreProcessReadData(readdat);
            _Session.Devices[DeviceID].ConfigData.ParseReturnMessage(procRead);


            Debug.WriteLine("[CSAFE] Executing Command Buffer Done");
        }


        //=========================================================================================================================
        // Execute Command In Frame Buffer
        //=========================================================================================================================
        private byte[] PreProcessReadData(byte[] ReadBuffer)
        {
            Debug.WriteLine("[CSAFE] Pre-processing Read Buffer...");
            byte[] tBuff = new byte[1024];
            int tLen = -1;
            bool gotEnd = false;
            bool nextStuffed = false;

            foreach (byte b in ReadBuffer)
            {
                //Console.Write("{0:x02}[{1,02}] ", b, tLen);
                if (tLen == -1)
                {
                    tLen = 0;
                    //Console.Write("RPT ");
                }
                else if (b == 0xF1)
                {
                    //Console.Write("SOF ");
                }
                else if (b == 0xF2)
                {
                    //Console.Write("EOF ");
                    gotEnd = true;
                    break;
                }
                else if (b == 0xF3)
                {
                    //Console.Write("STF ");
                    nextStuffed = true;
                }
                else if (nextStuffed)
                {
                    //Console.Write("UNS ");
                    tBuff[tLen++] = (byte)(0xF0 + (int)b);
                    nextStuffed = false;
                }
                else
                {
                    //Console.Write("STD ");
                    tBuff[tLen++] = b;
                }
                //Console.Write("    : ");
            }

            if (!gotEnd) return new byte[0];

            //Console.WriteLine(_Utils.DumpHex(tBuff));
            byte[] tRetPreChk = new byte[tLen];
            Array.Copy(tBuff, tRetPreChk, tLen);

            byte chksum = 0;
            for (int i = 0; i < tRetPreChk.Length - 1; i++) chksum ^= tRetPreChk[i];

            if (chksum != tRetPreChk[tRetPreChk.Length - 1]) return new byte[0];

            byte[] tReturn = new byte[tLen - 1];
            Array.Copy(tRetPreChk, tReturn, tLen - 1);

            Debug.WriteLine("[CSAFE] Done.");

            return tReturn;
        }

    }



    //=========================================================================================================================
    // CSAFE Commands that Control the State of the Slave
    // DOC REFERENCE 3.1.2.1
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Control_Commands
    {
        CSAFE_Session _Session;

        public CSAFE_Control_Commands(ref CSAFE_Session Session)
        {
            _Session = Session;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [null] Get Empty Frame
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdEmptyFrame()
        {
            byte[] cmdData = new byte[] { };

            Console.WriteLine("[CSAFE] Pushing cmdEmptyFrame.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x80] Get Status
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetStatus()
        {
            byte[] cmdData = new byte[] { 0x80 };

            Console.WriteLine("[CSAFE] Pushing cmdGetStatus.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x81] Go Reset
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdReset()
        {
            byte[] cmdData = new byte[] { 0x81 };

            Console.WriteLine("[CSAFE] Pushing cmdReset.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x82] Go Idle
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGoIdle()
        {
            byte[] cmdData = new byte[] { 0x82 };

            Console.WriteLine("[CSAFE] Pushing cmdGoIdle.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x85] Go In Use
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGoInUse()
        {
            byte[] cmdData = new byte[] { 0x85 };

            Console.WriteLine("[CSAFE] Pushing cmdGoInUse.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x87] Go Ready
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGoReady()
        {
            byte[] cmdData = new byte[] { 0x87 };

            Console.WriteLine("[CSAFE] Pushing cmdGoReady.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

    }


    //=========================================================================================================================
    // CSAFE Commands that Configure the Slave
    // DOC REFERENCE 3.1.2.2
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Configuration_Commands
    {
        CSAFE_Session _Session;

        public CSAFE_Configuration_Commands(ref CSAFE_Session Session)
        {
            _Session = Session;
        }



    }

    //=========================================================================================================================
    // CSAFE Commands that Configure Workout Data
    // DOC REFERENCE 3.1.2.3
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Configure_Workout_Commands
    {
        CSAFE_Session _Session;

        public CSAFE_Configure_Workout_Commands(ref CSAFE_Session Session)
        {
            _Session = Session;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x21] Set Horizontal distance goal
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdSetHorizontal(int HorizDistance, byte Units)
        {
            byte[] cmdData = new byte[] { 0x21, 0x03, 0xFF, 0xFF, Units };

            byte[] valBytes = BitConverter.GetBytes(HorizDistance);
            cmdData[2] = valBytes[0];
            cmdData[3] = valBytes[1];

            Console.WriteLine("[CSAFE] Pushing cmdSetHorizontal.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

 
        //-------------------------------------------------------------------------------------------------------------------------
        // [0x24] Set Machine Program Level
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdSetProgram(int Program)
        {
            byte[] cmdData = new byte[] { 0x24, 0x02, 0xFF, 0xFF };

            byte[] valBytes = BitConverter.GetBytes(Program);
            cmdData[2] = valBytes[0];
            cmdData[3] = valBytes[1];

            Console.WriteLine("[CSAFE] Pushing cmdSetProgram.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // [0x34] Set Power goal
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdSetPower(int Power, byte Units)
        {
            byte[] cmdData = new byte[] { 0x34, 0x03, 0xFF, 0xFF, Units };

            byte[] valBytes = BitConverter.GetBytes(Power);
            cmdData[2] = valBytes[0];
            cmdData[3] = valBytes[1];

            Console.WriteLine("[CSAFE] Pushing CSAFE_cmdSetPower.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }



    }


    //=========================================================================================================================
    // CSAFE Commands that Request General Information from the Slave
    // DOC REFERENCE 3.1.3.1
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Request_Data
    {
        CSAFE_Session _Session;

        public CSAFE_Request_Data(ref CSAFE_Session Session)
        {
            _Session = Session;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x9C] Equipment error code
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetErrorCode()
        {
            byte[] cmdData = new byte[] { 0x9C };

            Console.WriteLine("[CSAFE] Pushing cmdGetErrorCode.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

    }

    //=========================================================================================================================
    // CSAFE Commands that Request Workout Data
    // DOC REFERENCE 3.1.3.2
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Request_Workout_Data
    {
        CSAFE_Session _Session;

        public CSAFE_Request_Workout_Data(ref CSAFE_Session Session)
        {
            _Session = Session;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA0] Req Work Duration
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetTWork()
        {
            byte[] cmdData = new byte[] { 0xA0 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetTWork.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA1] Req Accumulated(for the workout) Distance(horizontal )
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetHorizontal()
        {
            byte[] cmdData = new byte[] { 0xA1 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetHorizontal.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA3] Req Accumulated Calories Burned
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetCalories()
        {
            byte[] cmdData = new byte[] { 0xA3 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetCalories.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA6] Req Current Pace
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetPace()
        {
            byte[] cmdData = new byte[] { 0xA6 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetPace.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA7] Current Cadence
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetCadence()
        {
            byte[] cmdData = new byte[] { 0xA7 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetCadence.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xB0] Current HR (BPM)
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetHRCur()
        {
            byte[] cmdData = new byte[] { 0xB0 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetHRCur.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xB4] Req Current Power
        //-------------------------------------------------------------------------------------------------------------------------
        public bool cmdGetPower()
        {
            byte[] cmdData = new byte[] { 0xB4 };

            Debug.WriteLine("[CSAFE] Pushing cmdGetPower.");
            return _Session.TxFrame.AddCommandBlock(cmdData);
        }

    }



    //=========================================================================================================================
    // CSAFE PARSING COMMANDS
    // DOC REFERENCE 3.1.3.1
    // https://web.archive.org/web/20060712183400/http://www.fitlinxx.com/csafe/Commands.htm
    //=========================================================================================================================
    public class CSAFE_Parse_Response
    {
        private AJRUtils _Utils = new AJRUtils();
        CSAFE_Session _Session;

        public CSAFE_Parse_Response(ref CSAFE_Session Session)
        {
            _Session = Session;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x9C] Equipment error code
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseErrorCode(byte[] Buffer, out int ErrorCode)
        {
            Debug.WriteLine("[CSAFE] Parsing parseErrorCode.");

            ErrorCode = 9999;

            if (Buffer[0] != 0x9C) return false;
            if (Buffer[1] != 0x03) return false;

            ErrorCode = BytesToInt(Buffer, 2, 3);
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0x1A] PM3 Extension Codes
        // TODO:  Figure out how to put this in the Concept2Api layer, don't know enough about C# yet to do that.
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parsePM3Extension(byte[] Buffer, out int PM3ExtCode)
        {
            Debug.WriteLine("[CSAFE] Parsing parsePM3Extension.");

            PM3ExtCode = 255;

            if (Buffer[0] != 0x1A) return false;

            PM3ExtCode = BytesToInt(Buffer, 1, 1);
            return true;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA0] Parse Work Duration
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseTWork(byte[] Buffer, out int Hours, out int Minutes, out int Seconds)
        {
            Debug.WriteLine("[CSAFE] Parsing parseTWork.");

            Hours = 255;
            Minutes = 255;
            Seconds = 255;

            if (Buffer[0] != 0xA0) return false;
            if (Buffer[1] != 0x03) return false;

            Hours = Buffer[2];
            Minutes = Buffer[3];
            Seconds = Buffer[4];

            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA1] Parse Accumulated(for the workout) Distance(horizontal )
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseHorizontal(byte[] Buffer, out int Distance, out int Units)
        {
            Debug.WriteLine("[CSAFE] Parsing parseGetHorizontal.");

            Distance = 65535;
            Units = 65535;

            if (Buffer[0] != 0xA1) return false;
            if (Buffer[1] != 0x03) return false;

            Distance = BytesToInt(Buffer, 2, 2);
            Units = Buffer[4];

            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA3] Parse Accumulated Calories Burned
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseCalories(byte[] Buffer, out int Calories)
        {
            Debug.WriteLine("[CSAFE] Parsing parseCalories.");

            Calories = 65535;

            if (Buffer[0] != 0xA3) return false;
            if (Buffer[1] != 0x02) return false;

            Calories = BytesToInt(Buffer, 2, 2);

            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA7] Parse Current Cadence
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseCadence(byte[] Buffer, out int Cadence, out int Units)
        {
            Debug.WriteLine("[CSAFE] Parsing parseCadence.");


            Cadence = 65535;
            Units = 65535;

            if (Buffer[0] != 0xA7) return false;
            if (Buffer[1] != 0x03) return false;

            Cadence = BytesToInt(Buffer, 2, 2);
            Units = Buffer[4];
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xA6] Parse Accumulated(for the workout) Distance(horizontal )
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parsePace(byte[] Buffer, out int Pace, out int Units)
        {
            Debug.WriteLine("[CSAFE] Parsing parseGetPace.");

            Pace = 65535;
            Units = 65535;

            if (Buffer[0] != 0xA6) return false;
            if (Buffer[1] != 0x03) return false;

            Pace = BytesToInt(Buffer, 2, 2);
            Units = Buffer[4];

            return true;
        }
        
        //-------------------------------------------------------------------------------------------------------------------------
        // [0xB0] Parse Accumulated Calories Burned
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parseHRCur(byte[] Buffer, out int HeartRate)
        {
            Debug.WriteLine("[CSAFE] Parsing parseHRCur.");

            HeartRate = 255;

            if (Buffer[0] != 0xB0) return false;
            if (Buffer[1] != 0x01) return false;

            HeartRate = Buffer[2];

            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // [0xB4] Current Power expenditure ( i.e. calories/min or watts )
        //-------------------------------------------------------------------------------------------------------------------------
        public bool parsePower(byte[] Buffer, out int Power, out int Units)
        {
            Debug.WriteLine("[CSAFE] Parsing parsePower.");

            Power = 65535;
            Units = 65535;

            if (Buffer[0] != 0xB4) return false;
            if (Buffer[1] != 0x03) return false;

            Power = BytesToInt(Buffer, 2, 2);
            Units = Buffer[4];

            return true;
        }


        private int BytesToInt(byte[] Buffer, int IndexStart, int Length)
        {
            byte[] tBytes = new byte[4];
            Array.Copy(Buffer, IndexStart, tBytes, 0, Length);
            return BitConverter.ToInt32(tBytes, 0);
        }

    }


}

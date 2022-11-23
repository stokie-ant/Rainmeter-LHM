/*
Copyright (C) 2022 Anthony Blakemore

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using LibreHardwareMonitor.Hardware;
using Rainmeter;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

// Overview: This plugin gets data from Libre Hardware Monitor lib

// Sample skin:
/*
[Rainmeter]
Update=1000
AccurateText=1
DynamicWindowSize=1
BackgroundMode=2
SolidColor=150,150,150

;; Base measure
;; All meters can reference this
[LHM]
Measure=Plugin
Plugin=LHM.dll
;; Some graphics drivers are known to eat memory when monitored
;; if this option is set to 1 gpu monitoring is disabled
DisableGPU=0
;; generating the report increases start time
;; if this option is set to 1 report will not be created
DisableReport=0

;;;;;;;;;;;;;;;;
;; Get a name ;;
;;;;;;;;;;;;;;;;

[meterLabelMotherboard]
Meter=String
X=0
Y=5
W=250
H=14
;; the GetName parameter can be found in the generated LHMReport.txt in the plugin dir
Text=[&LHM:GetName(0)]
;; DynamicVariables=1 is essential
DynamicVariables=1
FontSize=12
FontColor=140,252,124,255
AutoScale=1
AntiAlias=1

;;;;;;;;;;;;;;;;;
;; Get a value ;;
;;;;;;;;;;;;;;;;;

[CPU0Measure]
Measure=String
;; the GetValue parameter can be found in the generated LHMReport.txt in the plugin dir
String=[&LHM:GetValue(1,0)]
;; DynamicVariables=1 is essential
DynamicVariables=1
MinValue=0
MaxValue=100

[meterBarCPU0]
Meter=Bar
MeasureName=CPU0Measure
X=0
Y=0
W=250
H=5
BarColor=140,252,124,255
SolidColor=150,150,150,255
BarOrientation=Horizontal


[CPU1Measure]
Measure=String
;; the GetValue parameter can be found in the generated LHMReport.txt in the plugin dir
String=[&LHM:GetValue(1,1)]
;; DynamicVariables=1 is essential 
DynamicVariables=1
MinValue=0
MaxValue=100

[meterBarCPU1]
Meter=Bar
MeasureName=CPU1Measure
X=0
Y=20
W=250
H=5
BarColor=140,252,124,255
SolidColor=150,150,150,255
BarOrientation=Horizontal
*/

namespace LHM
{

    public class Measure
    {
        static public Rainmeter.API api;

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        public IntPtr buffer; //Prevent marshalAs from causing memory leaks by clearing this before assigning

        public static bool started = false;
        public static bool broken = false;
        public static bool isAdmin = false;
        public static bool EnableGpu = false;
        public static string emessage;
        public static DateTime TLastUpdate;

        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        public static Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = false,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true,
        };

        public void ThreadLoop()
        {
            if (api.ReadInt("DisableGPU", 0) == 0)
                computer.IsGpuEnabled = true;

            try
            {
                computer.Open();
            }
            catch (Exception e)
            {
                broken = true;
                emessage = e.ToString();
                return;
            }

            started = true;
            computer.Accept(new UpdateVisitor());

            if (api.ReadInt("DisableReport", 0) == 0)
                GenerateReport();

            while (true)
                foreach (IHardware hardware in computer.Hardware)
                {
                    hardware.Update();
                    Thread.Sleep(100);
                }
        }
        public Measure()
        {
            var id = WindowsIdentity.GetCurrent();
            if (id.Owner == id.User)
                return;

            isAdmin = true;
            // we run open() and update() in a thread so we don't soft lock waiting for the library
            Thread loop = new Thread(new ThreadStart(ThreadLoop));
            loop.Start();
        }

        private void GenerateReport()
        {
            string string1 = "Name";
            string1 = string1.PadRight(40) + "Value";
            string1 = string1.PadRight(55) + "Type";
            string1 = string1.PadRight(67) + "Get Name";
            string1 = string1.PadRight(87) + "Get Value\r\n";
            string string2;
            int idhw = 0;
            int idsubhw = 0;
            int idsen = 0;
            int idsubsen = 0;
            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();

                string1 = string1 + hardware.Name.PadRight(67) + "GetName(" + idhw + ")" + "\r\n";

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    string1 = string1 + subhardware.Name.PadRight(67) + "GetSubName(" + idhw + "," + idsubhw + ")" + "\r\n";

                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        string1 = string1 + sensor.Name.PadRight(40) + sensor.Value.ToString().PadRight(15) + sensor.SensorType.ToString().PadRight(12);
                        string2 = "GetName(" + idhw + "," + idsubhw + "," + idsubsen + ")";
                        string1 = string1 + string2.PadRight(20) + "GetValue(" + idhw + "," + idsubhw + "," + idsubsen + ")\r\n";
                        idsubsen++;
                    }
                    idsubhw++;
                    idsubsen = 0;
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    string1 = string1 + sensor.Name.PadRight(40) + sensor.Value.ToString().PadRight(15) + sensor.SensorType.ToString().PadRight(12);
                    string2 = "GetName(" + idhw + "," + idsen + ")";
                    string1 = string1 + string2.PadRight(20) + "GetValue(" +idhw + "," + idsen + ")\r\n";
                    idsen++;
                }

                idhw++;
                idsubhw = 0;
                idsen = 0;
                idsubsen = 0;
            }
            File.WriteAllText("LHMreport.txt", string1);
        }
    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Measure.api = (Rainmeter.API)rm;
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue) { }

        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0;
        }

        [DllExport]
        public static IntPtr GetName(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (!Measure.isAdmin)
            {
                Measure.api.Log(API.LogType.Error, "Plugin needs administrator privileges");
                return Marshal.StringToHGlobalUni("0");
            }

            if (Measure.broken)
            {
                Measure.api.Log(API.LogType.Error, Measure.emessage);
                return Marshal.StringToHGlobalUni("0");
            }

            if (!Measure.started)
                return Marshal.StringToHGlobalUni("0");

            try
            {
                if (argc == 1)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].Name);
                }
                if (argc == 2)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].Sensors[Convert.ToInt32(argv[1])].Name);
                }
                if (argc == 3)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].SubHardware[Convert.ToInt32(argv[1])].Sensors[Convert.ToInt32(argv[2])].Name);
                }

                Measure.api.Log(API.LogType.Error, "wrong number of parameters");
                return Marshal.StringToHGlobalUni("0");

            }
            catch (Exception e)
            {
                Measure.api.Log(API.LogType.Error, e.ToString());
                return Marshal.StringToHGlobalUni("0");
            }
        }

        [DllExport]
        public static IntPtr GetSubName(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (!Measure.isAdmin)
            {
                Measure.api.Log(API.LogType.Error, "Plugin needs administrator privileges");
                return Marshal.StringToHGlobalUni("0");
            }

            if (Measure.broken)
            {
                Measure.api.Log(API.LogType.Error, Measure.emessage);
                return Marshal.StringToHGlobalUni("0");
            }

            if (!Measure.started)
                return Marshal.StringToHGlobalUni("0");

            try
            {
                if (argc == 1)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].Name);
                }
                if (argc == 2)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].SubHardware[Convert.ToInt32(argv[1])].Name);
                }
                if (argc == 3)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].SubHardware[Convert.ToInt32(argv[1])].SubHardware[Convert.ToInt32(argv[2])].Name);
                }

                Measure.api.Log(API.LogType.Error, "wrong number of parameters");
                return Marshal.StringToHGlobalUni("0");

            }
            catch (Exception e)
            {
                Measure.api.Log(API.LogType.Error, e.ToString());
                return Marshal.StringToHGlobalUni("0");
            }
        }

        [DllExport]
        public static IntPtr GetValue(IntPtr data, int argc,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        {
            if (!Measure.isAdmin)
            {
                Measure.api.Log(API.LogType.Error, "Plugin needs administrator privileges");
                return Marshal.StringToHGlobalUni("0");
            }

            if (Measure.broken)
            {
                Measure.api.Log(API.LogType.Error, Measure.emessage);
                return Marshal.StringToHGlobalUni("0");
            }

            if (!Measure.started)
                return Marshal.StringToHGlobalUni("0");

            try
            {
                if (argc == 2)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].Sensors[Convert.ToInt32(argv[1])].Value.ToString());
                }
                if (argc == 3)
                {
                    return Marshal.StringToHGlobalUni(Measure.computer.Hardware[Convert.ToInt32(argv[0])].SubHardware[Convert.ToInt32(argv[1])].Sensors[Convert.ToInt32(argv[2])].Value.ToString());
                }
                else
                {
                    Measure.api.Log(API.LogType.Error, "wrong number of parameters");
                    return Marshal.StringToHGlobalUni("0");
                }
            }
            catch (Exception e)
            {
                Measure.api.Log(API.LogType.Error, e.ToString());
                return Marshal.StringToHGlobalUni("0");
            }
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            GCHandle.FromIntPtr(data).Free();
        }
    }
}

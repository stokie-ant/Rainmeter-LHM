## Rainmeter-LHM
Libre Hardware Monitor plugin for Rainmeter
 
 * Place LibreHardwareMonitorLib.dll and HidSharp.dll in the same folder as rainmeter.exe (C:\Program Files\Rainmeter)
 * Place LHM.dll in the plugins folder (C:\Users\username\AppData\Roaming\Rainmeter\Plugins)

```
// Sample skin:
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
```

## Building
* Using Visual Studio 2022
* Expects the rainmeter plugin sdk to be at ..\rainmeter-plugin-sdk
```
basedir
   |---Rainmeter-LHM
   |---rainmeter-plugin-sdk
```
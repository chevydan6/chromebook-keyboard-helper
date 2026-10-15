using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class ChromebookKeys
{
    delegate IntPtr Hook(int code, IntPtr message, IntPtr data);
    static readonly Hook callback = OnKey;
    static IntPtr hook;
    static bool enabled = true, search, searchUsed, forwardedWin;
    static readonly bool[] consumed = new bool[256];
    [StructLayout(LayoutKind.Sequential)] struct Key { public uint vk, scan, flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public InputUnion data; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion {
        [FieldOffset(0)] public KeyboardInput keyboard;
        [FieldOffset(0)] public MouseInput mouse;
    }
    [StructLayout(LayoutKind.Sequential)] struct KeyboardInput { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int x,y; public uint data,flags,time; public UIntPtr extra; }
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id, Hook proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr handle);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr handle, int code, IntPtr msg, IntPtr data);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)] static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError=true)] static extern uint SendInput(uint count, Input[] input, int size);
    static bool Held(int key) { return GetAsyncKeyState(key) < 0; }
    static void Send(int key, bool up) {
        var input = new Input { type=1, data=new InputUnion { keyboard=new KeyboardInput { vk=(ushort)key, flags=up?2u:0u } } };
        SendInput(1,new[]{input},Marshal.SizeOf(typeof(Input)));
    }
    static void Tap(int key) { Send(key,false);Send(key,true); }
    static void Chord(int modifier, int key) { Send(modifier,false);Tap(key);Send(modifier,true); }
    static void Brightness(int delta) {
        ThreadPool.QueueUserWorkItem(delegate {
            try {
                byte current=50;
                using(var s=new ManagementObjectSearcher("root\\wmi","SELECT * FROM WmiMonitorBrightness WHERE Active=True"))
                using(var rows=s.Get()) foreach(ManagementObject r in rows) using(r) {current=Convert.ToByte(r["CurrentBrightness"]);break;}
                byte target=(byte)Math.Max(1,Math.Min(100,current+delta));
                using(var s=new ManagementObjectSearcher("root\\wmi","SELECT * FROM WmiMonitorBrightnessMethods WHERE Active=True"))
                using(var rows=s.Get()) foreach(ManagementObject r in rows) using(r) {r.InvokeMethod("WmiSetBrightness",new object[]{1u,target});}
            } catch(Exception e) { WriteStatus("Brightness unavailable: "+e.Message); }
        });
    }
    static void Action(int number) {
        switch(number) {
            case 1:Tap(0xA6);break; // Browser back
            case 2:Tap(0xA8);break; // Refresh
            case 3:Tap(0x7A);break; // F11 / fullscreen
            case 4:Chord(0x5B,0x09);break; // Windows task view
            case 5:Brightness(-5);break;
            case 6:Brightness(5);break;
            case 7:ThreadPool.QueueUserWorkItem(delegate {try {Mic.Toggle();} catch(Exception e){WriteStatus("Microphone mute unavailable: "+e.Message);} });break;
            case 8:Tap(0xAD);break;
            case 9:Tap(0xAE);break;
            case 10:Tap(0xAF);break;
        }
    }
    static IntPtr OnKey(int code, IntPtr msg, IntPtr ptr) {
        if(code<0 || !enabled) return CallNextHookEx(hook,code,msg,ptr);
        Key k=(Key)Marshal.PtrToStructure(ptr,typeof(Key));
        if((k.flags&16)!=0) return CallNextHookEx(hook,code,msg,ptr);
        bool up=(k.flags&128)!=0;
        int vk=(int)k.vk;
        if(vk==0x5B || vk==0x5C) {
            if(!up) { if(!search) {search=true;searchUsed=false;forwardedWin=false;} }
            else {
                if(forwardedWin) Send(0x5B,true);
                else if(!searchUsed) {if(Held(0x12)) Tap(0x14);else Tap(0x5B);}
                search=false;searchUsed=false;forwardedWin=false;
            }
            return new IntPtr(1);
        }
        if(vk<256 && consumed[vk]) {
            if(up) consumed[vk]=false;
            else if(vk>=0x70 && vk<=0x79) { if(search) Tap(vk); else if(vk==0x74 || vk==0x75 || vk==0x78 || vk==0x79) Action(vk-0x70+1); }
            return new IntPtr(1);
        }
        if(!up && vk>=0x70 && vk<=0x79 && !Held(0x12) && !Held(0x11) && !Held(0x10)) {
            if(search) {searchUsed=true;Tap(vk);} else Action(vk-0x70+1);
            consumed[vk]=true;return new IntPtr(1);
        }
        // Vivaldi firmware can emit these special extended scan codes instead of F keys.
        if(!up && (k.flags&1)!=0 && !Held(0x12) && !Held(0x11) && !Held(0x10) && !search) {
            int action=k.scan==0x11?3:k.scan==0x12?4:k.scan==0x14?5:k.scan==0x15?6:k.scan==0x1b?7:0;
            if(action!=0 && vk<256) {Action(action);consumed[vk]=true;return new IntPtr(1);}
        }
        if(search && !up && !forwardedWin) {searchUsed=true;forwardedWin=true;Send(0x5B,false);}
        return CallNextHookEx(hook,code,msg,ptr);
    }
    static void WriteStatus(string value) {
        try {File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"helper-status.txt"),DateTime.Now.ToString("o")+" "+value);}catch{}
    }
    [STAThread] static void Main() {
        using(var mutex=new Mutex(false,"Local\\PujjoteenKeyboardHelper")) {
            if(!mutex.WaitOne(0))return;
            try {
                Application.EnableVisualStyles();
                hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);
                if(hook==IntPtr.Zero) {WriteStatus("Hook installation failed: "+Marshal.GetLastWin32Error());return;}
                var menu=new ContextMenuStrip();
                var toggle=new ToolStripMenuItem("ChromeOS top-row keys enabled"){Checked=true};
                toggle.Click+=delegate {
                    // Release any forwarded modifier before suspending the hook.
                    if(forwardedWin) Send(0x5B,true);
                    search=false;forwardedWin=false;searchUsed=false;Array.Clear(consumed,0,consumed.Length);
                    enabled=!enabled;toggle.Checked=enabled;
                };
                menu.Items.Add(toggle);menu.Items.Add("Exit",null,delegate{Application.Exit();});
                using(var tray=new NotifyIcon{Icon=System.Drawing.SystemIcons.Application,Text="Chromebook keyboard helper",Visible=true,ContextMenuStrip=menu}) {
                    WriteStatus("Running; Pujjoteen top-row mapping enabled; no keystroke logging");
                    Application.Run();tray.Visible=false;
                }
                menu.Dispose();
            } finally {if(forwardedWin)Send(0x5B,true);if(hook!=IntPtr.Zero)UnhookWindowsHookEx(hook);mutex.ReleaseMutex();}
        }
    }
}

[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IEnum { int EnumEndpoints(int a,int b,out IntPtr c); [PreserveSig]int GetDefault(int flow,int role,out IDevice device); }
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDevice { [PreserveSig]int Activate(ref Guid iid,int context,IntPtr param,out IntPtr volume); }
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IVolume {
 int Register(IntPtr p);int Unregister(IntPtr p);int Channels(out uint n);
 int SetLevel(float f,ref Guid g);int SetScalar(float f,ref Guid g);int GetLevel(out float f);int GetScalar(out float f);
 int SetChannel(uint n,float f,ref Guid g);int SetChannelScalar(uint n,float f,ref Guid g);int GetChannel(uint n,out float f);int GetChannelScalar(uint n,out float f);
 [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool b,ref Guid g);
 [PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool b);
}
static class Mic {
 public static void Toggle() {
    IEnum e=null;IDevice d=null;IVolume v=null;IntPtr p=IntPtr.Zero;
    try {
        e=(IEnum)new Enumerator();Marshal.ThrowExceptionForHR(e.GetDefault(1,1,out d));
        Guid iid=typeof(IVolume).GUID;Marshal.ThrowExceptionForHR(d.Activate(ref iid,23,IntPtr.Zero,out p));
        v=(IVolume)Marshal.GetObjectForIUnknown(p);bool muted;Marshal.ThrowExceptionForHR(v.GetMute(out muted));
        Guid g=Guid.Empty;Marshal.ThrowExceptionForHR(v.SetMute(!muted,ref g));
    } finally {if(v!=null)Marshal.ReleaseComObject(v);if(p!=IntPtr.Zero)Marshal.Release(p);if(d!=null)Marshal.ReleaseComObject(d);if(e!=null)Marshal.ReleaseComObject(e);}
 }
}

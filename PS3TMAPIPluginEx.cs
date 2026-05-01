using System;
using System.Diagnostics.Contracts;
using System.IO;
using ReClassNET.Core;
using ReClassNET.Debugger;
using ReClassNET.Memory;
using ReClassNET.Plugins;

namespace PS3TMAPIPlugin
{
    public class PS3TMAPIPluginExt : Plugin, ICoreProcessFunctions
    {
        private IPluginHost host;
        private TMAPI Target = new TMAPI();

        public override bool Initialize(IPluginHost host)
        {
            Contract.Requires(host != null);

            this.host = host ?? throw new ArgumentNullException(nameof(host));
            host.Process.CoreFunctions.RegisterFunctions("PS3TMAPIPlugin", this);

            return true;
        }

        public override void Terminate()
        {
            Target.Disconnect();
            host = null;
        }

        public bool ReadRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            uint addr = (uint)((ulong)address + (ulong)offset);

            try
            {
                buffer = Target.Ext.ReadBytes((uint)process, addr, size);

                if (buffer != null && buffer.Length > 0)
                    return true;
            }
            catch (Exception ex)
            {
                host.Logger.Log(ex);
                return false;
            }

            return false;
        }

        public bool WriteRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            uint addr = (uint)((ulong)address + (ulong)offset);

            try
            {
                Target.Ext.WriteBytes((uint)process, addr, buffer);
            }
            catch (Exception ex)
            {
                host.Logger.Log(ex);
                return false;
            }

            return true;
        }

        public bool IsProcessValid(IntPtr process)
        {
            return process != IntPtr.Zero;
        }

        public IntPtr OpenRemoteProcess(IntPtr process, ProcessAccess desiredAccess)
        {
            if (!Target.IsConnected) { return IntPtr.Zero; }

            return (IntPtr)process;
        }

        public void CloseRemoteProcess(IntPtr process)
        {
            if (!Target.IsConnected) { return; }
            Target.Disconnect();
        }

        public void EnumerateProcesses(EnumerateProcessCallback callback)
        {
            Target.Connect(); // Connect to the default target set in TargetManager

            if (callback != null)
            {
                if (!Target.IsConnected) { return; }

                Target.GetProcessList(out uint[] processIds);
                foreach (uint procId in processIds)
                {
                    EnumerateProcessData processData = new EnumerateProcessData()
                    {
                        Id = (IntPtr)procId,
                        Name = Path.GetFileName(Target.GetProcessPath(procId)),
                        Path = Target.GetProcessPath(procId),
                    };

                    callback(ref processData);
                }
            }
        }

        public void EnumerateRemoteSectionsAndModules(IntPtr process, EnumerateRemoteSectionCallback callbackSection, EnumerateRemoteModuleCallback callbackModule)
        {
            if (!Target.IsConnected) { return; }

            try
            {
                EnumerateRemoteModuleData moduleData = new EnumerateRemoteModuleData()
                {
                    BaseAddress = (IntPtr)0x0,
                    Size = (IntPtr)0x0,
                    Path = "Unknown",
                };

                callbackModule(ref moduleData);

                EnumerateRemoteSectionData sectionData = new EnumerateRemoteSectionData()
                {
                    Name = "Unknown",
                    ModulePath = "Unknown",
                    BaseAddress = (IntPtr)0x10000,
                    Size = (IntPtr)0x0,
                    Category = SectionCategory.Unknown,
                    Protection = SectionProtection.NoAccess,
                    Type = SectionType.Unknown,

                };

                callbackSection(ref sectionData);
            }
            catch (Exception ex)
            {
                host.Logger.Log(ex);
                throw new Exception("Failed to enumerate sections and modules:" + ex.ToString());
            }
        }

        public void ControlRemoteProcess(IntPtr process, ControlRemoteProcessAction action)
        {
            if (!Target.IsConnected) { return; }

            switch (action)
            {
                case ControlRemoteProcessAction.Suspend:
                    Target.ProcessStop((uint)process);
                    break;

                case ControlRemoteProcessAction.Resume:
                    Target.ProcessContinue((uint)process);
                    break;

                case ControlRemoteProcessAction.Terminate:
                    Target.ProcessKill((uint)process);
                    break;
            }
        }

        public bool AttachDebuggerToProcess(IntPtr process)
        {
            if (!Target.IsConnected) { return false; }

            Target.AttachProcess((uint)process);

            return true;
        }

        public void DetachDebuggerFromProcess(IntPtr process)
        {
            return;
        }

        public bool SetHardwareBreakpoint(IntPtr id, IntPtr address, HardwareBreakpointRegister register, HardwareBreakpointTrigger trigger, HardwareBreakpointSize size, bool set)
        {
            return false;
        }

        public bool AwaitDebugEvent(ref DebugEvent evt, int timeoutInMilliseconds)
        {
            return false;
        }

        public void HandleDebugEvent(ref DebugEvent evt)
        {
            return;
        }

        public int ConnectServer(string ip, short port)
        {
            return -1;
        }

        public bool OpenDumpFile(IntPtr dumpFilePath)
        {
            return false;
        }
    }
}

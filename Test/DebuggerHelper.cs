using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace Test {
    public static class DebuggerHelper {
        public static IEnumerable<KeyValuePair<Process, EnvDTE._DTE>> GetVSInstances() {
            IRunningObjectTable runningObjectTable = WinApiHelper.GetRunningObjectTable();
            IEnumMoniker enumMoniker;
            runningObjectTable.EnumRunning(out enumMoniker);
            IMoniker[] monikers = new IMoniker[1];
            for(enumMoniker.Reset(); enumMoniker.Next(1, monikers, IntPtr.Zero) == 0; ) {
                EnvDTE._DTE dte;
                Process dteProcess;
                try {
                    IBindCtx ctx = WinApiHelper.NewBindCtx();
                    string runningObjectName;
                    monikers[0].GetDisplayName(ctx, null, out runningObjectName);
                    if(!runningObjectName.StartsWith("!VisualStudio") && !runningObjectName.StartsWith("!WDExpress.DTE")) continue;
                    object runningObjectVal;
                    runningObjectTable.GetObject(monikers[0], out runningObjectVal);
                    dte = runningObjectVal as EnvDTE._DTE;
                    if(dte == null) continue;
                    int dteProcessId = int.Parse(runningObjectName.Split(':')[1]);
                    dteProcess = Process.GetProcessById(dteProcessId);
                } catch {
                    continue;
                }
                yield return new KeyValuePair<Process, EnvDTE._DTE>(dteProcess, dte);
            }
        }
        public static EnvDTE._DTE GetCurrentVSInstance() {
            if(!Debugger.IsAttached) return null;
            int currentProcessId = Process.GetCurrentProcess().Id;
            for(int i = 5; --i >= 0; ) {
                try {
                    return GetVSInstances().Where(p => p.Value.Debugger.DebuggedProcesses.OfType<EnvDTE.Process>().Where(d => d.ProcessID == currentProcessId).FirstOrDefault() != null).Select(p => p.Value).FirstOrDefault();
                } catch { }
            }
            return null;
        }
        public static void AttachDebuggerToProcess(EnvDTE._DTE dte, Process process, string program) {
            for(int i = 5; --i >= 0; ) {
                try {
                    var dteProcess = dte.Debugger.LocalProcesses.OfType<EnvDTE80.Process2>().First(p => p.ProcessID == process.Id);
                    dteProcess.Attach2(program);
                    return;
                } catch {
                    Thread.Sleep(200);
                }
            }
            throw new InvalidOperationException();
        }

        static class WinApiHelper {
            public static IRunningObjectTable GetRunningObjectTable() {
                IRunningObjectTable ret;
                if(Import.GetRunningObjectTable(0, out ret) != 0) return null;
                return ret;
            }
            public static IBindCtx NewBindCtx() {
                IBindCtx ret;
                if(Import.CreateBindCtx(0, out ret) != 0) return null;
                return ret;
            }

            class Import {
                [DllImport("ole32.dll")]
                public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);
                [DllImport("ole32.dll")]
                public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test;

namespace MonadaTest {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            var nunit = Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NUnit 2.6.4", "bin", "nunit-x86.exe"), Assembly.GetEntryAssembly().Location);
            var existingAgents = Process.GetProcessesByName("nunit-agent-x86").Select(x => x.Id).ToList();
            Process nunitAgentProcess = null;
            while(nunitAgentProcess == null) {
                nunitAgentProcess = Process.GetProcessesByName("nunit-agent-x86").Where(x => !existingAgents.Contains(x.Id)).FirstOrDefault();
                Thread.Sleep(100);
            }
            var vs = DebuggerHelper.GetCurrentVSInstance();
            if(vs != null)
                DebuggerHelper.AttachDebuggerToProcess(vs, nunitAgentProcess, "Managed (v4.5, v4.0)");
        }
    }
}

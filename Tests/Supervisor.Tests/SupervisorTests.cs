using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Supervisor.Tests {
    [TestClass]
    public class SupervisorTests {
        [TestMethod]
        public void ParseArgs() {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectPath = appDirectory.Substring(0, appDirectory.IndexOf("\\bin"));
            string testAppsPath = projectPath + "\\testApps.yaml";
            var args = Supervisor.ParseYamlConfig(testAppsPath);
            Assert.AreEqual(2, args.Count);
            var firstApp = new App {
                Name = "testApp",
                Program = "C:\\test.exe",
                Args = new List<string>() { "-a", "-f", "path to file" }
            };
            var secondApp = new App {
                Name = "testapp2",
                Program = "C:\\Program Files\\test.exe",
                Args = null
            };
            Assert.AreEqual(firstApp, args[0]);
            Assert.AreEqual(secondApp, args[1]);

            Assert.AreEqual(firstApp.GetArgs(), "-a -f \"path to file\"");
            Assert.AreEqual(secondApp.GetArgs(), null);

            Assert.AreEqual(firstApp.ToString(), "testApp: C:\\test.exe -a -f \"path to file\"");
            Assert.AreEqual(secondApp.ToString(), "testapp2: C:\\Program Files\\test.exe");
        }

        [TestMethod]
        public void MonitorProcess() {
            var app = new App {
                Name = "cmd",
                Program = "cmd.exe"
            };
            var monitor = new MonitorThread(app);
            try {
                // Process won't start until this thread is idle
                var waitForProcess = new Thread(delegate () {
                    for (int i = 0; i < 30; i++) {
                        if (monitor.Process != null)
                            break;
                        else
                            Thread.Sleep((i + 1) * 100);
                    }
                });
                waitForProcess.Start();
                waitForProcess.Join();
                Console.WriteLine("test");
                Assert.IsNotNull(monitor.Process);
                Assert.IsFalse(monitor.Process.HasExited);
                
                var process1 = monitor.Process;
                monitor.Process.Kill();
                waitForProcess = new Thread(delegate () {
                    for (int i = 0; i < 30; i++) {
                        if (monitor.Process != process1)
                            break;
                        else
                            Thread.Sleep((i + 1) * 100);
                    }
                });
                waitForProcess.Start();
                waitForProcess.Join();
                Assert.IsNotNull(monitor.Process);
                
                monitor.Stop();
                monitor.Join();
                Assert.IsTrue(monitor.Process.HasExited);
            } finally {
                monitor.Stop();
                monitor.Join();
            }
        }
    }
}

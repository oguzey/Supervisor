using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Linq;

namespace Supervisor
{
    public class Supervisor {
        static void Main(string[] args) {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a YAML config file with applications to start and watch. For example:");
                Console.WriteLine("supervisor myapps.yaml");
                return;
            }
            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine("WARNING: The argument '{0}' will be ingored", args[i]);
            }
            var apps = ParseYamlConfig(args[0]);
            if (apps.Count == 0) {
                Console.WriteLine("The YAML config file '{0}' is invalid", args[0]);
                Console.WriteLine("Please fix your YAML config file");
                return;
            }

            var threads = new List<MonitorThread>(apps.Count);

            for (int i = 0; i < apps.Count; i++) {
                var t = new MonitorThread(apps[i]);
                threads.Add(t);
            }

            Console.WriteLine("Press enter to stop all processes.");
            Console.ReadLine();
            for (int i = 0; i < threads.Count; i++) {
                threads[i].Stop();
            }
            for (int i = 0; i < threads.Count; i++) {
                threads[i].Join();
            }
        }

        public static List<App> ParseYamlConfig(string configFilePath) {
            var apps = new List<App>();
            try
            {
                var configString = File.ReadAllText(configFilePath);
                var configStream = new StringReader(configString);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var appsFromConfigs = deserializer.Deserialize<List<App>>(configStream);
                foreach (var app in appsFromConfigs)
                {
                    Console.WriteLine("Found an app: {0}", app.Name);
                    Console.WriteLine("  program: {0}", app.Program);
                    Console.WriteLine("  args: {0}", app.GetArgs());
                    Console.WriteLine("");
                }
                apps = appsFromConfigs;
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException)
                {
                    Console.WriteLine("The file '{0}' does not exist", configFilePath);
                    Console.WriteLine("Please provide yaml config file");
                }
                else if (e is UnauthorizedAccessException)
                {
                    Console.WriteLine("Config file access denied. An error: {0}", e.Message);
                }
                else if (e is YamlDotNet.Core.SemanticErrorException)
                {
                    Console.WriteLine("Could not parse the YAML file. Syntax error: {0}", e.Message);
                }
                else if (e is YamlDotNet.Core.YamlException)
                {
                    Console.WriteLine("The mandatory key not found. An error: {0}", e.Message);
                    Console.WriteLine("Please use the following schema:");
                    Console.WriteLine("-name: App1");
                    Console.WriteLine("  program: C:\\myapp.exe");
                    Console.WriteLine("  args:");
                    Console.WriteLine("  - myarg1");
                    Console.WriteLine("  - \"-flag\"");
                    Console.WriteLine("-name: App2");
                    Console.WriteLine("  program: C:\\projects\\myapp2.exe");
                }
                else
                {
                    Console.WriteLine("Unknown error occurred: {0}", e.ToString());
                }
                apps.Clear();
                return apps;
            }
            return apps;
        }
    }

    public class MonitorThread {
        public readonly App Application;
        
        public Process Process {
            get;
            private set;
        }

        private bool IsCancelled;

        private readonly Thread Thread;

        public MonitorThread(App args) {
            Application = args;
            Thread = new Thread(DoMonitor);
            Thread.Start();
        }

        private Process StartProcess(App app) {
            if (app.GetArgs() == null) {
                return Process.Start(Application.Program);
            }
            else {
                return Process.Start(Application.Program, app.GetArgs());
            }
        }

        private void DoMonitor() {
            Console.WriteLine("Start app {0}", Application.ToString());
            Process = StartProcess(Application);
            while (true) {
                if (Process.WaitForExit(1000) && !IsCancelled) {
                    Console.WriteLine("Restart app {0}", Application.ToString());
                    Process = StartProcess(Application);
                }
                if (IsCancelled) {
                    if (!Process.HasExited)
                        Process.Kill();
                    return;
                }
            }
        }

        public void Stop() {
            IsCancelled = true;
        }

        public void Join() {
            if (Thread.IsAlive)
                Thread.Join();
        }

        public bool Join(TimeSpan timeout) {
            if (Thread.IsAlive)
                return Thread.Join(timeout);
            else
                return true;
        }
    }

    public class App
    {
        public string Program;
        public string Name;
        public List<string> Args;

        public string GetArgs() {
            if (Args == null) {
                return null;
            }
            string args = "";
            foreach (string arg in Args) {
                if (arg.Contains(" ")) {
                    args += "\"" + arg + "\" ";
                } else {
                    args += arg + " ";
                }
            }
            return args.Remove(args.Length - 1);
        }
        public override bool Equals(object obj) {
            if (obj is App) {
                var b = obj as App;
                return Program == b.Program && Name == b.Name 
                        && ((Args == b.Args) || Args.SequenceEqual(b.Args));
            } else
                return false;
        }

        public override string ToString() {
            var result = Name + ": " + Program;
            if (Args != null)
                result += " " + GetArgs();
            return result;
        }

        public override int GetHashCode() {
            var result = Name.GetHashCode() + Program.GetHashCode();
            result = 31 * result + Args.GetHashCode();
            return result;
        }
    }
}

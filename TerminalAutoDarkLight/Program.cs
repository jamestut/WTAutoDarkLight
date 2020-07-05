using System;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace TerminalAutoDarkLight
{
    static class Program
    {
        [DllImport("Advapi32.dll")] static extern UInt32 RegNotifyChangeKeyValue(UIntPtr hkey, UInt32 watchSubtree, UInt32 notifyFilter, UIntPtr handler, UInt32 async);
        [DllImport("Advapi32.dll")] static extern UInt32 RegOpenKeyExA(UIntPtr hkey, string subkey, UInt32 options, UInt32 samDesired, ref UIntPtr result);
        [DllImport("Advapi32.dll")] static extern UInt32 RegQueryValueExA(UIntPtr hkey, string valueName, UIntPtr reserved, ref UInt32 dwType, ref UInt32 data, ref UInt32 dataSize);
        [DllImport("user32.dll")] static extern UInt32 MessageBoxA(UIntPtr hwnd, string text, string caption, UInt32 type);

        static string _ConfigPath;
        static string _MyConfigPath;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                MessageBoxA((UIntPtr)0, "Usage: TerminalAutoDarkLight (windows terminal config path) (scheme path)", "Help", 0);
                return;
            }
            _ConfigPath = args[0];
            _MyConfigPath = args[1];

            UIntPtr HKCU = (UIntPtr)0x80000001;
            bool lightMode = false;

            UIntPtr hkeyPersona = (UIntPtr)0;
            if (RegOpenKeyExA(HKCU, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", 0, 0x20019, ref hkeyPersona) != 0)
            {
                MessageBoxA((UIntPtr)0, "Cannot open registry key", "Error", 0);
                return;
            }

            // set the initial mode on startup
            UInt32 regType = 0, regValue = 0, regValueLen = 4;
            if (RegQueryValueExA(hkeyPersona, "AppsUseLightTheme", (UIntPtr)0, ref regType, ref regValue, ref regValueLen) != 0)
            {
                MessageBoxA((UIntPtr)0, "Cannot read color scheme registry value", "Error", 0);
                return;
            }
            lightMode = regValue != 0;
            ChangeColorScheme(lightMode);


            while (RegNotifyChangeKeyValue(hkeyPersona, 0, 4, (UIntPtr)0, 0) == 0)
            {
                // read app theme setting
                UInt32 err = RegQueryValueExA(hkeyPersona, "AppsUseLightTheme", (UIntPtr)0, ref regType, ref regValue, ref regValueLen);
                if (err == 0)
                {
                    bool newLightMode = regValue != 0;
                    if (lightMode != newLightMode)
                    {
                        lightMode = newLightMode;
                        ChangeColorScheme(lightMode);
                    }
                }
            }
        }

        static void ChangeColorScheme(bool lightMode)
        {
            IDictionary<string, object> targetObj;
            try
            {
                targetObj = JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(_ConfigPath), new DictionaryConverter());
                var myObj = JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(_MyConfigPath), new DictionaryConverter());
                myObj = (IDictionary<string, object>)myObj[lightMode ? "light" : "dark"];
                string[] targets = new string[] { "profiles", "defaults" };
                string[] properties = new string[] { "colorScheme", "cursorColor", "selectionBackground" };
                var currObj = targetObj;
                foreach (var target in targets)
                {
                    if (!currObj.ContainsKey(target))
                    {
                        currObj.Add(target, new Dictionary<string, object>());
                    }
                    currObj = (Dictionary<string, object>)currObj[target];
                }

                // replace properties
                foreach (var prop in properties)
                {
                    if (!myObj.ContainsKey(prop))
                    {
                        currObj.Remove(prop);
                    }
                    else
                    {
                        currObj[prop] = myObj[prop];
                    }
                }


            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Assembly.GetCallingAssembly().GetName().Name,
                    $"Error loading or modifying configuration data. Error was: {ex}.", EventLogEntryType.Error, 1);
                return;
            }
            // prepare to write
            string configData = JsonConvert.SerializeObject(targetObj, Formatting.Indented) + "\n";
            // retry several times before giving up
            const int retryCount = 10;
            for (int i = 0; i < retryCount; ++i)
            {
                try
                {
                    using (var fs = new FileStream(_ConfigPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        // empty the existing contents first
                        fs.SetLength(0);
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.Write(configData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(Assembly.GetCallingAssembly().GetName().Name,
                        $"Error writing out new configuration file. Error: {ex}", EventLogEntryType.Error, 1);
                    // retry
                    Thread.Sleep(1000);
                }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Management;
using System.Collections.Generic;

namespace NetworkResetTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var adapters = GetNetworkAdapters();

            if (adapters.Count == 0)
            {
                Console.WriteLine("未发现可用的网卡。");
                return;
            }

            // 添加一个伪网卡用于“全部重置”
            adapters.Add(new NetworkAdapterInfo
            {
                Name = "[全部重置所有网卡]",
                Description = "执行所有网卡的 IP 重置"
            });

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.WriteLine("请选择要重置 IP 的网卡（使用 ↑ ↓ 选择，Enter 确认）：\n");

                for (int i = 0; i < adapters.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }

                    Console.WriteLine($"{i + 1}. {adapters[i].Name} ({adapters[i].Description})");

                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow)
                    selectedIndex = (selectedIndex - 1 + adapters.Count) % adapters.Count;
                else if (key == ConsoleKey.DownArrow)
                    selectedIndex = (selectedIndex + 1) % adapters.Count;

            } while (key != ConsoleKey.Enter);

            Console.Clear();

            if (selectedIndex == adapters.Count - 1)
            {
                Console.WriteLine("正在重置所有网卡 IP...\n");

                ExecuteIpconfig("/release");
                ExecuteIpconfig("/renew");
            }
            else
            {
                var selected = adapters[selectedIndex];
                Console.WriteLine($"正在重置网卡: {selected.Name}\n");

                ExecuteIpconfig("/release \"" + selected.Name + "\"");
                ExecuteIpconfig("/renew \"" + selected.Name + "\"");
            }

            Console.WriteLine("\n操作完成。按任意键退出...Powered by github.com/rivenww");
            Console.ReadKey();
        }

        static void ExecuteIpconfig(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(output);
                if (output.Contains("没有为 DHCP 启用适配器"))
                {
                    Console.WriteLine($"\n选中的网卡 当前为静态 IP，无法自动重置。");
                    Console.WriteLine("请前往 控制面板 → 网络和共享中心 或 Windows 设置 → 网络 中启用 DHCP。");
                }
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine("错误: " + error);
            }
        }

        static List<NetworkAdapterInfo> GetNetworkAdapters()
        {
            var result = new List<NetworkAdapterInfo>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled = TRUE");

            foreach (ManagementObject obj in searcher.Get())
            {
                string name = obj["NetConnectionID"] as string;
                string desc = obj["Description"] as string;

                if (!string.IsNullOrEmpty(name))
                {
                    result.Add(new NetworkAdapterInfo
                    {
                        Name = name,
                        Description = desc,
                    });
                }
            }

            return result;
        }

        class NetworkAdapterInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }
}

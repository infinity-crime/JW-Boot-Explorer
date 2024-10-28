using System;
using System.Management;
using LibreHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JW_Boot_Explorer
{
    public partial class MainWindow : Window
    {
        /* Переменная _computer (LibreHardwareMonitor) позволяет открыть мониторинг для 
         разных типов оборудования */
        private readonly Computer _computer;
        /* Таймер WPF для выполнения задачи через определенные интервалы */
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent(); // Стандартный метод WPF инициализ. элементы интерфейса из xaml

            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };

            _computer.Open(); // запуск мониторинга для доступа к данным

            /* Получение названия процессора и видеокарты при загрузке программы */
            CpuNameTextBlock.Text = $"CPU name: {GetCpuName()}";
            GpuNameTextBlock.Text = $"GPU name: {GetGpuName()}";

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };

            /* подписываемся на событие Tick, чтобы метод UpdateHardwareInfo
             вызывался через указанные интервалы */
            _timer.Tick += UpdateHardwareInfo;
            _timer.Start();
        }

        private void UpdateHardwareInfo(object sender, EventArgs e)
        {
            var cpuUsage = GetCpuUsage();
            CpuUsageTextBlock.Text = $"CPU Usage: {cpuUsage}%";

            var memoryInfo = GetMemoryInfo();
            RamUsageTextBlock.Text = $"RAM Usage: Использованная память - {memoryInfo.used} MB / Свободная память - {memoryInfo.total} MB";

            /* Так как через System.Managment не получить данные о видеокарте
            будем получать через LHM */
            foreach(var hardware in _computer.Hardware)
            {
                hardware.Update();
                if(hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Name == "GPU Core")
                        {
                            GpuUsageTextBlock.Text = $"GPU Usage: {sensor.Value.GetValueOrDefault():F2}%";
                        }
                    }
                }
            }
        }

        private float GetCpuUsage() // метод позволяющий получить загрузку процессора в момент вызова
        {
            float cpuUsage = 0;

            var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor");
            foreach(var obj in searcher.Get())
            {
                cpuUsage += Convert.ToSingle(obj["LoadPercentage"]);
            }

            return cpuUsage;
        }

        private (float used, float total) GetMemoryInfo()
        {
            float totalMemory = 0;
            float freeMemory = 0;

            var searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                totalMemory = Convert.ToSingle(obj["TotalVisibleMemorySize"]) / 1024;
                freeMemory = Convert.ToSingle(obj["FreePhysicalMemory"]) / 1024;
            }

            var usedMemory = totalMemory - freeMemory;
            return (usedMemory, totalMemory);
        }

        private string GetCpuName()
        {
            string cpuName = "Unknown CPU";

            var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                cpuName = obj["Name"].ToString();
            }

            return cpuName;
        }

        private string GetGpuName()
        {
            string gpuName = "Unknown GPU";

            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    gpuName = hardware.Name;
                }
            }

            return gpuName;
        }
    }
}

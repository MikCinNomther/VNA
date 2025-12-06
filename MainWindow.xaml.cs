using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;  // 添加这个命名空间
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;  // 添加这个命名空间

namespace VNA
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged  // 实现接口
    {
        private string _fi;
        string[] args = null;
        public MainWindow(string[] argv = null)
        {
            InitializeComponent();
            DataContext = this;
            args = argv;
        }

        public string Fi
        {
            get { return _fi; }
            set
            {
                if (_fi != value)
                {
                    _fi = value;
                    OnPropertyChanged();  // 调用通知方法
                    new Task(async () =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            Shower.Text = File.ReadAllText(value);
                        }));
                    }).Start();
                }
            }
        }

        // 实现 INotifyPropertyChanged 接口
        public event PropertyChangedEventHandler PropertyChanged;

        // 通知属性更改的方法
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // 改进的拖放处理，添加安全检查
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null && files.Length > 0)
            {
                Fi = files[0];  // 这会触发 OnPropertyChanged 通知 UI 更新
            }
        }
        public static string ShellVersion = "STD1.0";


        public static void RunShell(string xmlText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xmlText))
                {
                    MessageBox.Show("脚本内容为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlText);

                // 验证 ShellVersion
                XmlNode shellVersionNode = xmlDocument.SelectSingleNode("//*[@id='ShellVersion']");
                if (shellVersionNode == null)
                {
                    MessageBox.Show("错误：XML 中没有找到 ShellVersion 元素",
                        "脚本错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string sv = shellVersionNode.InnerText?.Trim();
                if (string.IsNullOrEmpty(sv))
                {
                    MessageBox.Show("ShellVersion 元素内容为空！",
                        "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (sv != ShellVersion)
                {
                    MessageBox.Show($"不支持该脚本的版本(\"{sv}\")！\n当前支持的版本是：{ShellVersion}",
                        "版本不匹配", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                // 获取 Information 内容
                string Information = "";
                XmlNode informationNode = xmlDocument.SelectSingleNode("//Infomation[@id='Information']");
                if (informationNode != null)
                {
                    Information = informationNode.InnerText.Trim();
                }
                else
                {
                    Information = "未找到脚本描述信息";
                }

                // 显示确认对话框
                if (MessageBox.Show($"--------\r\n{Information}\r\n--------\r\n你希望执行这个脚本吗？",
                    "脚本详情", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                // 执行脚本步骤
                XmlNodeList stepNodes = xmlDocument.SelectNodes("//Step");
                if (stepNodes == null || stepNodes.Count == 0)
                {
                    MessageBox.Show("脚本中没有找到任何执行步骤", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 遍历执行所有步骤
                for (int i = 0; i < stepNodes.Count; i++)
                {
                    XmlNode stepNode = stepNodes[i];
                    string stepName = $"步骤 {i + 1}/{stepNodes.Count}";

                    // 执行当前步骤
                    bool stepResult = ExecuteStep(stepNode, stepName);

                    if (!stepResult)
                    {
                        // 步骤执行失败，直接终止脚本
                        return; // 终止脚本执行
                    }
                }

                MessageBox.Show("脚本执行完成！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误信息：\r\n{ex.Message}\r\n{ex.StackTrace}",
                    "运行错误！", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool ExecuteStep(XmlNode stepNode, string stepName)
        {
            try
            {
                // 获取步骤属性
                string condition = stepNode.Attributes?["Condition"]?.Value;
                string disCondition = stepNode.Attributes?["DisCondition"]?.Value;
                string returnOnError = stepNode.Attributes?["ReturnOnError"]?.Value;
                string returnDialog = stepNode.Attributes?["ReturnDialog"]?.Value;

                // 检查 Condition 条件
                if (!string.IsNullOrEmpty(condition))
                {
                    bool conditionMet = File.Exists(condition);
                    Console.WriteLine($"{stepName}: 检查条件文件 '{condition}' => {(conditionMet ? "存在" : "不存在")}");

                    if (!conditionMet)
                    {
                        // 条件不满足
                        if (returnOnError?.ToLower() == "true")
                        {
                            // 需要返回错误
                            string errorMsg = !string.IsNullOrEmpty(returnDialog) ?
                                returnDialog : $"条件不满足: 文件 '{condition}' 不存在";
                            MessageBox.Show(errorMsg, "执行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false; // 终止执行
                        }
                        else
                        {
                            // 不需要返回，直接跳过此步骤
                            Console.WriteLine($"{stepName}: 条件不满足，跳过此步骤");
                            return true; // 继续执行下一个步骤
                        }
                    }
                }

                // 检查 DisCondition 条件
                if (!string.IsNullOrEmpty(disCondition))
                {
                    bool disConditionMet = File.Exists(disCondition);
                    Console.WriteLine($"{stepName}: 检查禁止条件文件 '{disCondition}' => {(disConditionMet ? "存在" : "不存在")}");

                    if (disConditionMet)
                    {
                        // 禁止条件满足
                        if (returnOnError?.ToLower() == "true")
                        {
                            // 需要返回错误
                            string errorMsg = !string.IsNullOrEmpty(returnDialog) ?
                                returnDialog : $"禁止条件满足: 文件 '{disCondition}' 存在";
                            MessageBox.Show(errorMsg, "执行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false; // 终止执行
                        }
                        else
                        {
                            // 不需要返回，直接跳过此步骤
                            Console.WriteLine($"{stepName}: 禁止条件满足，跳过此步骤");
                            return true; // 继续执行下一个步骤
                        }
                    }
                }

                // 条件检查通过，执行命令
                XmlNodeList commandNodes = stepNode.SelectNodes("Command");
                if (commandNodes == null || commandNodes.Count == 0)
                {
                    Console.WriteLine($"{stepName}: 没有找到任何命令");
                    return true; // 没有命令也算成功
                }

                // 执行每个命令
                for (int i = 0; i < commandNodes.Count; i++)
                {
                    string command = commandNodes[i].InnerText.Trim();
                    if (!string.IsNullOrEmpty(command))
                    {
                        Console.WriteLine($"{stepName} 命令 {i + 1}/{commandNodes.Count}: {command}");

                        // 执行命令
                        bool commandResult = ExecuteCommand(command);

                        if (!commandResult)
                        {
                            // 命令执行失败
                            if (returnOnError?.ToLower() == "true")
                            {
                                // 需要返回错误
                                string errorMsg = !string.IsNullOrEmpty(returnDialog) ?
                                    returnDialog : $"命令执行失败: {command}";
                                MessageBox.Show(errorMsg, "执行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false; // 终止执行
                            }
                            else
                            {
                                // 不需要返回，继续执行下一个命令
                                Console.WriteLine($"{stepName}: 命令执行失败，但设置为不返回错误，继续执行");
                            }
                        }
                    }
                }

                return true; // 步骤执行成功
            }
            catch (Exception ex)
            {
                string returnDialog = stepNode.Attributes?["ReturnDialog"]?.Value;
                string errorMsg = !string.IsNullOrEmpty(returnDialog) ?
                    returnDialog : $"步骤执行异常: {ex.Message}";
                MessageBox.Show(errorMsg, "执行错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static bool ExecuteCommand(string command)
        {
            try
            {
                // 这里需要实现具体的命令执行逻辑
                // 根据命令类型执行不同的操作

                if (command.StartsWith("git clone"))
                {
                    // 执行 Git 克隆
                    return ExecuteProcessCommand("git", command.Substring(3));
                }
                else if (command.StartsWith("move"))
                {
                    // 执行移动文件
                    return ExecuteMoveCommand(command);
                }
                else if (command.StartsWith("python"))
                {
                    // 执行 Python 脚本
                    return ExecuteProcessCommand("python", command.Substring(6));
                }
                else
                {
                    // 默认使用 CMD 执行
                    return ExecuteProcessCommand("cmd.exe", $"/C {command}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"命令执行异常: {command}, 错误: {ex.Message}");
                return false;
            }
        }

        private static bool ExecuteMoveCommand(string command)
        {
            try
            {
                // 简化实现：直接调用系统 move 命令
                return ExecuteProcessCommand("cmd.exe", $"/C {command}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移动文件失败: {ex.Message}");
                return false;
            }
        }

        private static bool ExecuteProcessCommand(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine($"命令输出: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"错误输出: {error}");
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令失败: {fileName} {arguments}, 错误: {ex.Message}");
                return false;
            }
        }

        

        
        private static bool ExecuteGitCommand(string command)
        {
            // 简化实现，实际项目中需要完整实现
            Console.WriteLine($"执行 Git 命令: {command}");

            // 这里可以调用 Process.Start 来实际执行命令
            // 暂时返回 true 表示成功
            return true;
        }


        private static bool ExecutePythonCommand(string command)
        {
            Console.WriteLine($"执行 Python 命令: {command}");

            // 这里可以调用 Process.Start 来执行 Python 脚本
            // 暂时返回 true 表示成功
            return true;
        }

        private static bool ExecuteSystemCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine($"命令输出: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"错误输出: {error}");
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行系统命令失败: {ex.Message}");
                return false;
            }
        }
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if(Shower.Text != string.Empty)
            RunShell(Shower.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (args.Length > 0)
            {
                Fi = args[0];
            }
        }
    }
}
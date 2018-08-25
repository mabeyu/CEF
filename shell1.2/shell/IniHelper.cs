using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shell
{
    class IniHelper
    {
        // 声明INI文件的写操作函数 WritePrivateProfileString()
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        // 声明INI文件的读操作函数 GetPrivateProfileString()
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

        [System.Runtime.InteropServices.DllImport("shell32")]
        public static extern IntPtr ShellExecute(IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            ShowCommands nShowCmd);
        private string sPath = null;
        public IniHelper()
        {
        }
        public void ImportHelper(string path)
        {
            this.sPath = path;
        }
        public enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }
        public void WriteValue(string section, string key, string value)
        {
            // section=配置节，key=键名，value=键值，path=路径
            WritePrivateProfileString(section, key, value, sPath);
        }

        public string ReadValue(string section, string key)
        {
            // 每次从ini中读取多少字节
            System.Text.StringBuilder temp = new System.Text.StringBuilder(255);
            // section=配置节，key=键名，temp=上面，path=路径
            GetPrivateProfileString(section, key, "", temp, 255, sPath);
            if (temp.Length == 0)
            {
                return "0";
            }
            else
            {
                return temp.ToString().TrimEnd(';');
            }
        }
        //异常打印到日志
        public static void WriteLog(Exception ex)
        {
            string path = Environment.CurrentDirectory + '\\' + "Log";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += '\\' + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            //把异常信息输出到文件
            StreamWriter fs = new StreamWriter(path, true);
            fs.WriteLine("当前时间：" + DateTime.Now.ToString());
            fs.WriteLine("异常信息：" + ex.Message);
            fs.WriteLine("异常对象：" + ex.Source);
            fs.WriteLine("调用堆栈：\n" + ex.StackTrace.Trim());
            fs.WriteLine("触发方法：" + ex.TargetSite);
            fs.WriteLine();
            fs.Close();
        }
        public static void WriteInfo(string str)
        {
            string path = Environment.CurrentDirectory + '\\' + "Info";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += '\\' + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            //把异常信息输出到文件
            StreamWriter fs = new StreamWriter(path, true);
            fs.WriteLine("当前时间：" + DateTime.Now.ToString());
            fs.WriteLine("打印信息：" + str);
            fs.WriteLine();
            fs.Close();
        }
    }
}

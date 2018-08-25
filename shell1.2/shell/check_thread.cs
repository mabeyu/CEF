using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace shell
{
    class check_thread
    {
        public check_thread(string dir, string type, websocket mWebsocket)
        {
            this.dir = dir;
            this.type = type;
            this.mWebsocket = mWebsocket;
        }
        public string dir { get; set; }
        public string type { get; set; }
        Thread check_fsw_3;
        websocket  mWebsocket;
        //监测是否有RESULT文件夹生成
        public void Check_dir()
        {
            try
            {
                if (type == "grade")//制版
                {
                    bool open = true;
                    while (open)
                    {
                        string[] filedir = Directory.GetFiles(dir.ToString(), "*.*", SearchOption.AllDirectories);
                        foreach (string s in filedir)
                        {
                            if (Regex.Match(s.ToLower(), "result").Value != "")
                            {
                                string watch_path = dir + @"\result";//监测器3的监测路径
                                thread_checkBuidFile td = new thread_checkBuidFile(watch_path, type);
                                check_fsw_3 = new Thread(new ThreadStart(td.Check_dir));
                                check_fsw_3.IsBackground = true;
                                check_fsw_3.Start();
                                open = false;
                            }
                        }
                        Thread.Sleep(2000);
                    }
                }
                else if (type == "maskList")
                {
                    string watch_path = dir + @"\result";//监测器3的监测路径
                    thread_checkBuidFile td = new thread_checkBuidFile(watch_path, type);
                    check_fsw_3 = new Thread(new ThreadStart(td.Check_dir));
                    check_fsw_3.IsBackground = true;
                    check_fsw_3.Start();
                }
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
    class thread_fsw2
    {
        IniHelper mIni;
        public string path { get; set; }
        public string type { get; set; }
        public FileSystemWatcher fsw_2 = null;
        websocket mWebsocket;
        public thread_fsw2(string type, string path, websocket mWebsocket)
        {
            this.path = path;
            this.type = type;
            this.mWebsocket = mWebsocket;
            mIni = new IniHelper();
            mIni.ImportHelper(System.Environment.CurrentDirectory + "\\Param.ini");
        }
        public void Thread_Watch_fsw2()
        {
            try
            {
                fsw_2 = new FileSystemWatcher();
                fsw_2.Created += new FileSystemEventHandler(fsw_2_Created);
                fsw_2.Filter = "*.*";
                fsw_2.Path = path;
                fsw_2.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show("请稍后再试");
            }
        }
        public void fsw_2_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                //打开程序之前先检测有没有打开过相同的程序
                Process[] proList = Process.GetProcesses();
                foreach (Process process in proList)
                {
                    if (process.ProcessName == "ETCOM_WIBU_500"|| process.ProcessName == "ETMark_WIBU_TD")
                    {
                        if (!Form1.dic_process.ContainsKey(process.Id))
                        {
                            Form1.dic_process.TryAdd(process.Id, process.Id.ToString());
                        }
                    }
                }
                //----
                string filePath = path + @"\" + e.Name.ToString();
                string businessid = path.Substring(path.LastIndexOf(@"\") + 1);
                if (type == "grade" && e.Name.ToString().Contains("自动放码"))//以指定程序打开指定文件
                {
                    //监测result文件夹
                    check_thread ct = new shell.check_thread(path, type, mWebsocket);
                    Thread check_dir = new Thread(new ThreadStart(ct.Check_dir));
                    check_dir.IsBackground = true;
                    check_dir.Start();

                    string exe = mIni.ReadValue("ET", "gradeEXE");
                    //string exe = @"C:\Windows\notepad.exe";
                    IniHelper.ShellExecute(IntPtr.Zero, "open", exe, filePath, null, IniHelper.ShowCommands.SW_SHOWNORMAL);
                    //创建新的线程监测打开的进程是否关闭                      
                    Process[] procesList = Process.GetProcesses();
                    foreach (Process process in procesList)
                    {
                        if (process.ProcessName == "ETCOM_WIBU_500")
                        {
                            if (!Form1.dic_process.ContainsKey(process.Id))
                            {
                                Form1.dic_process.TryAdd(process.Id, businessid);
                                //创建线程监测进程是否关闭
                                thread_Process tp = new thread_Process(type, businessid, process.Id);
                                Thread td = new Thread(new ThreadStart(tp.Thread_Watch_process));
                                td.IsBackground = true;
                                td.Start();
                            }
                        }
                    }
                }
                else if (type == "maskList" && (e.Name.ToString().Contains("自动排料")||e.Name.EndsWith("prj")))
                {
                    //监测result文件夹
                    check_thread ct = new shell.check_thread(path, type, mWebsocket);
                    Thread check_dir = new Thread(new ThreadStart(ct.Check_dir));
                    check_dir.IsBackground = true;
                    check_dir.Start();

                    ////修改oif文件里面的[define] output内容
                    //IniHelper mIni2 = new IniHelper();
                    //mIni2.ImportHelper(filePath);
                    //string newvalue = str + "\\result2";
                    //mIni2.WriteValue("Define", "output", newvalue);
                    string exe = mIni.ReadValue("ET", "maskListEXE");
                    IniHelper.ShellExecute(IntPtr.Zero, "open", exe, filePath, null, IniHelper.ShowCommands.SW_SHOWNORMAL);
                    Thread.Sleep(2000);
                    //创建新的线程监测打开的进程是否关闭
                    Process[] procesList = Process.GetProcesses();
                    foreach (Process process in procesList)
                    {
                        if (process.ProcessName == "ETMark_WIBU_TD")
                        {
                            if (!Form1.dic_process.ContainsKey(process.Id))
                            {
                                Form1.dic_process.TryAdd(process.Id, businessid);
                                //创建线程监测进程是否关闭
                                thread_Process tp = new thread_Process(type, businessid, process.Id);
                                Thread td = new Thread(new ThreadStart(tp.Thread_Watch_process));
                                td.IsBackground = true;
                                td.Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
    //监测生成的文件 并将result文件夹里面的所有文件添加进列表
    class thread_checkBuidFile
    {
        public delegate void OpenUpload(int s);
        public static OpenUpload openUpload;
        public string path { get; set; }
        public string type { get; set; }
        public thread_checkBuidFile(string path, string type)
        {
            this.path = path;
            this.type = type;
        }
        public void Check_dir()
        {
            try
            {
                CheckFile(type);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show(ex.Message);
            }
        }
        private void CheckFile(string type)
        {            
            string fileType = type == "grade" ? "*.emf" : "*.nc";
            ConcurrentDictionary<string, string> dic_ = type == "grade" ? Form1.dic_gradeFiles : Form1.dic_maskListFiles;
            bool open = true;
            while (open)
            {
                string[] filedir = Directory.GetFiles(path.ToString(), fileType, SearchOption.AllDirectories);
                if (filedir.Length != 0)
                {
                    string orderid = path.Replace("\\result", "");
                    orderid = orderid.Substring(orderid.LastIndexOf("\\") + 1);
                    dic_.TryAdd(orderid, orderid);
                    openUpload(2);
                    open = false;
                }
                Thread.Sleep(2000);
            }
        }              
    }
    //进程监测类  检查进程是否存在
    class thread_Process  
    {
        public int processid { get; set; }
        public string businessid { get; set; }
        public string type { get; set; }
        public thread_Process(string type, string businessid, int processid)
        {
            this.businessid = businessid;
            this.processid = processid;
            this.type = type;
        }
        public void Thread_Watch_process()
        {
            bool isExist = true;
            while(isExist)
            {
                Process[] prcessList = Process.GetProcesses();
                foreach(Process p in prcessList)
                {
                    isExist = p.Id == processid ? true : false;
                    if (isExist)
                        break;
                }
                if(!isExist)
                {
                    if (type == "grade")
                    {
                        string val = "";
                        Form1.dic_gradeFiles.TryRemove(businessid, out val);
                        Form1.dic_process.TryRemove(processid, out val);
                    }
                    if (type == "maskList")
                    {
                        string val = "";
                        Form1.dic_maskListFiles.TryRemove(businessid, out val);
                        Form1.dic_process.TryRemove(processid, out val);
                    }
                }
                Thread.Sleep(2000);
            }
        }
    }
    class thread_fsw4  //打开POD
    {
        public string path { get; set; }
        public FileSystemWatcher fsw_4 = null;
        HTTP mHTTP;
        public thread_fsw4(string dir, HTTP http)
        {
            this.path = dir;
            mHTTP = http;
        }
        public void Thread_Watch_fsw4()
        {
            fsw_4 = new FileSystemWatcher();
            fsw_4.Created += new FileSystemEventHandler(fsw_4_Created);
            fsw_4.Filter = "*.pod";
            fsw_4.Path = path;
            fsw_4.EnableRaisingEvents = true;
        }
        public void fsw_4_Created(object sender, FileSystemEventArgs e)
        {
            string a = this.fsw_4.Path + @"\" + e.Name.ToString();
            string str = this.fsw_4.Path + @"\";
            string[] filedir = Directory.GetFiles(str, "*.*", SearchOption.AllDirectories);
            if (filedir != null)
            {
                Process.Start(a);//打开ETCAD
            }                        
        }
    }
}

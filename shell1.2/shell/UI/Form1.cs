using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.WinForms;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing;
using ComposerSDK;

namespace shell
{
    public partial class Form1 : Form
    {
        #region 全局变量
        public ChromiumWebBrowser wb = null;
        //HTTP mHTTP = new HTTP();
        websocket mWebsocket = new websocket();
        UploadParam uploadParam = new UploadParam();
        IniHelper mIni = new IniHelper();
        Thread check_fsw_2;
        delegate void SystemDelegate(int s);
        SystemDelegate reload,openOrderUI;
        public static ConcurrentDictionary<int, string> dic_process = new ConcurrentDictionary<int, string>();//进程
        public static ConcurrentDictionary<string, string> dic_gradeFiles = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> dic_maskListFiles = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> dic_id = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> dic_creator = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, UpLoad> dic_upLoad = new ConcurrentDictionary<string, UpLoad>();

        //缩放等级
        double zoomLevel = 0;

        #region 截图全局变量

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, System.Windows.Forms.Keys vk);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd,int id );
        [Flags]
        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Windows = 8
        }
        [DllImport("User32.dll")]
        public static extern IntPtr GetForegroundWindow();     //获取活动窗口句柄  
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);   //获取线程ID  
        public static int processId = 0;
        Bitmap g_bit;
        #endregion
        #region 灯光拣选全局变量
        XGate xgate;
        public bool Xopen = false;
        delegate void XgateDelegate();
        XgateDelegate executeLigthSort;
        public static ConcurrentQueue<Light> que_light = new ConcurrentQueue<Light>();
        #endregion
        #endregion
        public Form1()
        {
            Creatfolder(@"D:\ETCAD");
            Creatfolder(@"D:\ETCAD\制版");
            Creatfolder(@"D:\ETCAD\排料");
            Creatfolder(@"D:\ETCAD\裁剪");
            Creatfolder(@"D:\ETCAD\绣花操作");
            Creatfolder(@"D:\ETCAD\绣花列表");
            Creatfolder(@"D:\ETCAD\款式工艺模板");
            Creatfolder(@"D:\ETCAD\款式物料模板");
            InitializeComponent();
            mIni.ImportHelper(System.Environment.CurrentDirectory + "\\Param.ini");
            SetStyle(ControlStyles.DoubleBuffer, true);
            openOrderUI = new SystemDelegate(OpenOrderUI);
            reload = new SystemDelegate(ReloadPage);
            thread_checkBuidFile.openUpload = new thread_checkBuidFile.OpenUpload(InvokeMainThread);
            websocket.refresh = new websocket.InterfaceNote(InvokeMainThread);
            websocket.order = new websocket.InterfaceNote(InvokeMainThread);
        }
        private void Form1_Load(object sender, EventArgs a)
        {
            try
            {
                //读取websocket地址     
                //string[] locIps = websocket.GetLocIps();
                //string locip = locIps[0];
                if (mWebsocket.StartListen("127.0.0.1", "8088"))//开始监听...
                {
                    //MessageBox.Show("监听成功");
                }
                else
                    MessageBox.Show("请检查网络和端口！");
                //CEF
                cefInit();
                //wb = new ChromiumWebBrowser(@"http://www.iqiyi.com/");
                string web_url = mIni.ReadValue("DPS参数", "url");
                if (web_url == "0")
                    MessageBox.Show("参数配置错误");
                wb = new ChromiumWebBrowser(web_url);
                wb.Dock = DockStyle.Fill;
                panel1.Controls.Add(wb);
                wb.DownloadHandler = new DownloadHandler();
                //监听按键事件
                GlobalKeyboardHook gHook = new GlobalKeyboardHook();
                gHook.KeyDown += new KeyEventHandler(gHook_KeyDown);
                foreach (Keys key in Enum.GetValues(typeof(Keys)))
                {
                    gHook.HookedKeys.Add(key);
                }
                gHook.hook();
                //热键注册
                RegisterHotKey(this.Handle, 7890, KeyModifiers.Alt | KeyModifiers.Control, Keys.X);
                //页面缩放
                //wb.SetZoomLevel(0.5);
                //灯光拣选
                executeLigthSort = new XgateDelegate(StartCon);
                websocket.executeLigthSort = new websocket.XgateDelegate(ExecuteLigthSort);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show("网页打开失败", "错误提示");
            }
        }

        /// <summary>
        /// 打开订单新窗口
        /// </summary>
        /// <param name="orderid"></param>
        private void OpenUpload(string orderid)
        {
            UpLoad upload = new UpLoad(mWebsocket);
            if (Form1.dic_upLoad.Count > 0)
            {
                foreach (KeyValuePair<string, UpLoad> kv in Form1.dic_upLoad)//关闭其他窗口并移除
                {
                    kv.Value.Close();
                    kv.Value.Dispose();
                    websocket w = new websocket();
                    UpLoad u = new UpLoad(w);
                    Form1.dic_upLoad.TryRemove(kv.Key, out u);
                }
            }
            Form1.dic_upLoad.TryAdd(orderid, upload);
            upload.Show();
        }
        /// <summary>
        /// 唤醒界面线程 --- 刷新 --界面
        /// </summary>
        private void InvokeMainThread(int s)
        {
            switch (s)
            {
                case 1:
                    this.Invoke(reload,s);
                    break;
                case 2:
                    this.Invoke(openOrderUI,s);
                    break;
            }
        }
        /// <summary>
        /// 刷新页面
        /// </summary>
        private void ReloadPage(int s)
        {
            try
            {
                wb.GetBrowser().Reload();
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        private void OpenOrderUI(int s)
        {
            try
            {
                UpLoad upload = new UpLoad(mWebsocket);
                if (Form1.dic_upLoad.Count > 0)
                {
                    foreach (KeyValuePair<string, UpLoad> kv in Form1.dic_upLoad)//关闭其他窗口并移除
                    {
                        kv.Value.Close();
                        kv.Value.Dispose();
                        websocket w = new websocket();
                        UpLoad u = new UpLoad(w);
                        Form1.dic_upLoad.TryRemove(kv.Key, out u);
                    }
                }
                string str = "";
                Form1.dic_upLoad.TryAdd(str, upload);
                upload.Show();
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }
        /// <summary>
        /// 设置cookie信息
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookies"></param>
        public static void SetCefCookies(string url, CookieCollection cookies)
        {
            Cef.GetGlobalCookieManager().SetStoragePath(Environment.CurrentDirectory, true);
            foreach (System.Net.Cookie c in cookies)
            {
                var cookie = new CefSharp.Cookie
                {
                    Creation = DateTime.Now,
                    Domain = c.Domain,
                    Name = c.Name,
                    Value = c.Value,
                    Expires = c.Expires
                };
                Task<bool> task = Cef.GetGlobalCookieManager().SetCookieAsync(url, cookie);
                while (!task.IsCompleted)
                {
                    continue;
                }
                bool b = task.Result;
            }
        }
        /// <summary>
        /// 处理制版信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                string fsw_path = this.fileSystemWatcher1.Path + @e.Name.ToString();
                //dic_files.TryAdd(e.Name.ToString(), e.Name.ToString());
                thread_fsw2 td = new thread_fsw2("grade", fsw_path, mWebsocket);
                check_fsw_2 = new Thread(new ThreadStart(td.Thread_Watch_fsw2));
                check_fsw_2.IsBackground = true;
                check_fsw_2.Start();

                websocket.Dwon(mWebsocket.spath, mWebsocket.gradeList);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                if (ex.Message.Contains("集合已修改"))
                    MessageBox.Show("请稍后再试");
                return;
            }
        }
        /// <summary>
        /// 处理裁剪信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileSystemWatcher2_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                websocket.Dwon(mWebsocket.path, mWebsocket.maskList);
                //复制文件到指定目录
                string sourcePath = this.fileSystemWatcher2.Path + @"\" + mWebsocket.businessId;
                string destPath = @"D:\";
                HTTP.CopyFile(sourcePath, destPath);
                MessageBox.Show("裁床可执行NC文件已经保存到D盘根目录!");
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 处理绣花操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileSystemWatcher3_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                websocket.Dwon(mWebsocket.path, mWebsocket.embOpera);
                Process.Start(this.fileSystemWatcher3.Path + @"\" + mWebsocket.businessId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("下载失败", "错误提示");
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        public void Creatfolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private void fileSystemWatcher5_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                websocket.Dwon(mWebsocket.path, mWebsocket.embList);
                Process.Start(this.fileSystemWatcher5.Path + @"\" + mWebsocket.businessId);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                MessageBox.Show("下载失败", "错误提示");
            }
        }
        /// <summary>
        /// 处理排料信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileSystemWatcher4_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                string fsw_path = this.fileSystemWatcher4.Path + "\\" + @e.Name.ToString();//订单号文件夹
                thread_fsw2 td = new thread_fsw2("maskList", fsw_path, mWebsocket);
                check_fsw_2 = new Thread(new ThreadStart(td.Thread_Watch_fsw2));
                check_fsw_2.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                IniHelper.WriteLog(ex);
            }
            try
            {
                websocket.Dwon(mWebsocket.spath, mWebsocket.blowDown);
            }
            catch (Exception ex)
            {
                MessageBox.Show("下载失败", "错误提示");
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// CEF初始化参数设置
        /// </summary>
        public void cefInit()
        {
            try
            {
                //清空缓存
                if(Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "cache\\Cache"))
                {
                    string[] filedir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "cache\\Cache", "*.*", SearchOption.TopDirectoryOnly);
                    if (filedir != null)
                    {
                        foreach (string fileName in filedir)
                        {
                            File.Delete(fileName);
                        }
                    }
                }
                CefSettings setting = new CefSharp.CefSettings();
                //关闭日志文件
                setting.LogSeverity = CefSharp.LogSeverity.Disable;
                //Flash设置
                //setting.CefCommandLineArgs.Add("enable-npapi","1");
                //string flashPath = @"C:\Users\shallow\AppData\Local\Google\Chrome\User Data\PepperFlash\28.0.0.137\pepflashplayer.dll";
                //setting.CefCommandLineArgs.Add("ppapi-flash-path", flashPath);
                //setting.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
                //屏幕闪烁设置
                setting.CefCommandLineArgs.Add("disable-gpu", "1"); // 禁用gpu
                setting.Locale = "zh-CN";                           // 设置语言
                //缓存路径
                setting.CachePath = AppDomain.CurrentDomain.BaseDirectory + "cache\\";
                setting.PersistSessionCookies = true;
                CefSharp.Cef.Initialize(setting, true, null);

                string cookieDirec = AppDomain.CurrentDomain.BaseDirectory + "cache\\cookie\\";
                ICookieManager cookieManager = CefSharp.Cef.GetGlobalCookieManager();
                cookieManager.SetStoragePath(cookieDirec, false);
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// F5刷新网页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void gHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.ContainsFocus)
            {
                if (e.KeyValue == 116)
                {
                    wb.GetBrowser().Reload();
                }
                else if(e.KeyValue==117)
                {
                    try
                    {
                        UpLoad upload = new UpLoad(mWebsocket);
                        if (Form1.dic_upLoad.Count > 0)
                        {
                            foreach (KeyValuePair<string, UpLoad> kv in Form1.dic_upLoad)//关闭其他窗口并移除
                            {
                                kv.Value.Close();
                                kv.Value.Dispose();
                                websocket w = new websocket();
                                UpLoad u = new UpLoad(w);
                                Form1.dic_upLoad.TryRemove(kv.Key, out u);
                            }
                        }
                        string str = "";
                        Form1.dic_upLoad.TryAdd(str, upload);
                        upload.Show();
                    }
                    catch (Exception ex)
                    {
                        IniHelper.WriteLog(ex);
                        return;
                    }
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            g_bit = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(g_bit);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), g_bit.Size);
            Thread.Sleep(100);
        }
        /// <summary>
        /// 热键获取事件
        /// </summary>
        private void GlobalKeyProc()
        {
            Thread.Sleep(100);
            backgroundWorker1.RunWorkerAsync();
        }
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                //hotkey pressed
                case 0x0312:
                    if (m.WParam.ToString() == "7890")
                    {
                        GlobalKeyProc();
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            IntPtr inp = GetForegroundWindow();
            GetWindowThreadProcessId(inp, out processId);
            Thread.Sleep(100);
            SnapForm sf = new SnapForm(g_bit);
            sf.ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 7890);
        }
        #region 灯光拣选
        private void ExecuteLigthSort()
        {
            this.Invoke(executeLigthSort);
        }
        private void StartCon()
        {
            string ip = mIni.ReadValue("控制器参数", "ip");
            if (!Xopen)
                StartController(ip);
            disPlay900();
        }
        void xgate_DeviceErrorCallback(XGate xgate, Device PTLDevice, Exception error)
        {
            this.BeginInvoke(new MethodInvoker(() =>
            {
                IniHelper.WriteLog(error);
            }));
        }
        void xgate_PickedInforCallback(XGate xgate, Device PTLDevice, string[] contents, int[] pickedQuantities, bool FnPressed)
        {
            try
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    websocket.LightSortReturn(int.Parse(contents[0]), pickedQuantities[0]);
                    xgate.Clear900U(PTLDevice.Address);
                    //xgate.Close();
                    string info = "设备类型：" + PTLDevice + "\r\n" + "货位号：" + contents[0] + "\r\n" + "拣货数量：" + pickedQuantities[0];
                    IniHelper.WriteInfo(info);
                    if (que_light.Count != 0)
                        disPlay900();
                }));
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        void Do(MethodInvoker action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        public void disPlay900()
        {
            Light temp_light = null;
            que_light.TryDequeue(out temp_light);
            //数据读取...........................................................................            
            var skuinfo = new List<string>();          //前面数字
            var quantities = new List<int>();          //后面数字
            skuinfo.Add(temp_light.id.ToString());
            quantities.Add(Convert.ToInt32(temp_light.qty));
            //显示
            bool isSucceed= this.xgate.Display900U(temp_light.machineNum, skuinfo, quantities, 0x2d, 0, true);
            if(!isSucceed)
            {
                string str = "机架号：" + temp_light.machineNum.ToString() + "\r\n 数量：" + skuinfo;
                IniHelper.WriteInfo(str);
            }
        }
        //启动控制器
        public void StartController(string ip)
        {
            this.Do(() =>
            {
                this.xgate = new XGate(ip, 2);
                this.xgate.DeviceErrorCallback += xgate_DeviceErrorCallback;
                this.xgate.PickedInforCallback += xgate_PickedInforCallback;
                //关闭
                for (int i = 1; i < 11; i++)
                {
                    this.xgate.Clear900U(i);
                    this.xgate.Close();
                    //this.xgate.ClearAll();
                }
                //int address = this.TryParseAddress();
                //打开
                for (int i = 1; i < 11; i++)
                {
                    this.xgate.Add900U(i, i / 100);
                    this.xgate.Start();
                    //this.xgate.ClearAll();
                }
                Xopen = true;
            });
        }
        private void toolStripMenuItem1_Click_2(object sender, EventArgs e)
        {
            zoomLevel += 0.5;
            wb.SetZoomLevel(zoomLevel);
        }
        #endregion

        /// <summary>
        /// 实现下载的接口
        /// </summary>
        internal class DownloadHandler : IDownloadHandler
        {
            public void OnBeforeDownload(IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
            {
                if (!callback.IsDisposed)
                {
                    using (callback)
                    {
                        callback.Continue(@"D:\dps下载文件\" +downloadItem.SuggestedFileName,
                            showDialog: false);
                    }
                }
            }
            public void OnDownloadUpdated(IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
            {
                //downloadItem.IsCancelled = false;  
            }
            public bool OnDownloadUpdated(CefSharp.DownloadItem downloadItem)
            {
                return false;
            }
        }
    }
}

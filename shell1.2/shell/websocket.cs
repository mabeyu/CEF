using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Web;

namespace shell
{
    /// <summary>
    /// 访问的客户端类
    /// </summary>
    public class ClientOP
    {
        public DateTime dt;//最后访问的时间
        Socket socket = null;//最后访问的socket

        public string str = "hehe";
        string ip = "";//客户端IP地址
        string port = "";//客户端端口号
        string msg = "";//客户端发送的信息
        int pactype = -1;//数据类型
        bool fin = false;//数据包是否结束
        bool isflash = false;//是否是采用flash的websocket
        string devid = "";
        /// <summary>
        /// 声明ClietnOP类
        /// </summary>
        public ClientOP()
        {
            dt = DateTime.Now;
        }

        #region 属性访问

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        public string IP
        {
            get { return ip; }
            set { ip = value; }
        }

        public string Devid
        {
            get { return devid; }
            set { devid = value; }
        }
        /// <summary>
        /// 客户端端口号
        /// </summary>
        public string Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        /// 获取接收消息最后的时间
        /// </summary>
        public DateTime Time
        {
            get { return dt; }
            set { dt = value; }
        }

        /// <summary>
        /// 获取时间的字符串
        /// </summary>
        public string TimeStr
        {
            get { return dt.ToString("yyyy-MM-dd HH:mm:ss"); }
        }

        /// <summary>
        /// 获取用户的socket
        /// </summary>
        public Socket cSocket
        {
            get { return socket; }
            set { socket = value; }
        }

        /// <summary>
        /// 数据包类型
        /// </summary>
        public int OpCode
        {
            get { return pactype; }
            set { pactype = value; }
        }
        /// <summary>
        /// 是否最后一个数据包
        /// </summary>
        public bool Pac_Fin
        {
            get { return fin; }
            set { fin = value; }
        }
        /// <summary>
        /// 数据包消息内容
        /// </summary>
        public string Pac_Msg
        {
            get { return msg; }
            set { msg = value; }
        }

        /// <summary>
        /// 是否flashweb
        /// </summary>
        public bool Pac_Flash
        {
            get { return isflash; }
            set { isflash = value; }
        }

        #endregion
    }

    /// <summary>
    /// websocket数据包解析
    /// </summary>
    public class websocket
    {
        /// <summary>
        /// 客户端map
        /// </summary>
        public static ConcurrentDictionary<string, ClientOP> dic_clients = new ConcurrentDictionary<string, ClientOP>();
        /// <summary>
        /// 客户端消息队列
        /// </summary>
        public static ConcurrentQueue<ClientOP> que_msgs = new ConcurrentQueue<ClientOP>();
        public delegate void mDelegate2(string i);
        public static mDelegate2 SystemNote;

        bool isStop = false;//中心是否停止监听
        Socket ListenSocket;//接收客户端请求的socket
        Thread check_logout = null;//检查客户端是否已经离线
        public List<string> IP = new List<string>();

        #region DPS全局变量
        public HttpListener sHttp;
        public string path, spath, newpath;
        public string businessId;
        public string upload_file;
        public List<string> gradeList = new List<string>();
        public List<string> maskList = new List<string>();
        public List<string> embList = new List<string>();
        public List<string> embOpera = new List<string>();
        public List<string> blowDown = new List<string>();
        public delegate void InterfaceNote(int s);
        public static InterfaceNote reloadPage, refresh, order;  //刷新页面
        public delegate void XgateDelegate();
        public static XgateDelegate executeLigthSort;                          //灯光拣选
        CardReader cr = new CardReader();
        #endregion
        
        /// <summary>
        /// 声明WSClass
        /// </summary>
        /// 
        public websocket()
        {
            //string[] locIps=GetLocIps();
            //this.localIP = locIps[0];
            if (check_logout == null || !check_logout.IsAlive)
            {
                check_logout = new Thread(new ThreadStart(Thread_Check_Logout));
                check_logout.IsBackground = true;
                check_logout.Start();
            }
        }

        #region 静态方法

        /// <summary>
        /// 获取本地IP列表
        /// </summary>
        /// <returns></returns>
        public static string[] GetLocIps()
        {
            List<string> arr = new List<string>();
            IPAddress[] arrIPAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in arrIPAddresses)
            {
                if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                    arr.Add(ip.ToString());
            }
            return arr.ToArray();
        }

        #endregion

        #region 异步socket监听

        private void StartAccept()//开始监听请求
        {
            if (!isStop)
            {
                SocketAsyncEventArgs AcceptEventArg = new SocketAsyncEventArgs();
                AcceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(Asyn_Complete);
                if (!ListenSocket.AcceptAsync(AcceptEventArg))//false为同步完成，手动触发
                {
                    ProcessAccept(AcceptEventArg);
                }
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)//客户端连接请求
        {
            if (acceptEventArgs.SocketError != SocketError.Success || isStop)
            {
                if (!isStop)
                    StartAccept();
                closeConnect(acceptEventArgs.AcceptSocket);
                acceptEventArgs = null;
                return;
            }
            StartAccept();//继续等待下一次连接请求
            SocketAsyncEventArgs ReceiveEventArgs = new SocketAsyncEventArgs();
            //
            ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Asyn_Complete);
            ReceiveEventArgs.SetBuffer(new byte[65536], 0, 65536);//分配数据缓存空间
            ReceiveEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;
            acceptEventArgs = null;
            StartReceive(ReceiveEventArgs);
        }

        private void StartReceive(SocketAsyncEventArgs receiveEventArgs)//开始接受数据
        {
            if (!isStop)
            {
                if (receiveEventArgs.AcceptSocket.Connected)
                {
                    if (!receiveEventArgs.AcceptSocket.ReceiveAsync(receiveEventArgs))//false为同步完成，手动触发
                    {
                        ProcessReceive(receiveEventArgs);
                    }
                }
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs receiveEventArg)//接收数据
        {
            if (receiveEventArg.SocketError != SocketError.Success || receiveEventArg.BytesTransferred == 0)
            {
                closeConnect(receiveEventArg.AcceptSocket);
                receiveEventArg = null;
                return;
            }
            int len = receiveEventArg.BytesTransferred;//接收到的数据包长度
            byte[] data_pac = new byte[len];
            Array.Copy(receiveEventArg.Buffer, receiveEventArg.Offset, data_pac, 0, len);//将接收到的数据包放入data_pac中
            Func<bool, byte[]> appendData = ok =>
            {
                byte[] newdata;
                if (receiveEventArg.UserToken != null)
                {
                    byte[] tmp = (byte[])receiveEventArg.UserToken;
                    newdata = new byte[len + tmp.Length];
                    Array.Copy(tmp, 0, newdata, 0, tmp.Length);
                    Array.Copy(data_pac, 0, newdata, tmp.Length, len);
                    if (ok)//true时表示所有数据接收完毕
                        receiveEventArg.UserToken = null;
                }
                else
                    newdata = data_pac;
                return newdata;
            };
            //if (receiveEventArg.AcceptSocket.Available != 0)
            //    receiveEventArg.UserToken = appendData(false);
            //else
            {
                data_pac = appendData(true);
                string msg = "";
                ClientOP cp = new ClientOP();
                cp.cSocket = receiveEventArg.AcceptSocket;
                if (Analyze(data_pac, len, cp))
                {
                    if (!cp.Pac_Flash)//当为flash请求策略文件时不加入消息队列
                        que_msgs.Enqueue(cp);//将接收的消息加入队列
                    msg = cp.Pac_Msg;
                    if (cp.OpCode == 1)//服务器处理客户端发送的信息
                    {
                        //处理注册信息
                        pro_data(cp.Pac_Msg, cp);
                    }
                    else if (cp.OpCode == 101)//自定义发送信息
                    {
                        //Send(cp.cSocket, shakehand());
                    }
                    else if (cp.OpCode == 10)//处理心跳包
                    {
                        pro_data(cp.Pac_Msg, cp);
                    }
                }
                else
                {
                    cp = null;
                    closeConnect(receiveEventArg.AcceptSocket);
                }
            }
            StartReceive(receiveEventArg);//继续等待下一次接收数据
        }

        private void Asyn_Complete(object sender, SocketAsyncEventArgs e)//当socket的请求、接收操作完成时
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept: ProcessAccept(e); break;
                case SocketAsyncOperation.Receive: ProcessReceive(e); break;
                default: throw new ArgumentException("无效动作！");
            }
        }

        #endregion


        #region 函数方法

        /// <summary>
        /// 向用户发送数据
        /// </summary>
        /// <param name="msg">消息内容</param>
        /// <param name="socket">用户的套接字</param>
        /// <returns></returns>
        public bool SendToClient(string msg, Socket socket)
        {
            try
            {
                Send(socket, msg);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <param name="ip">监听的ip地址</param>
        /// <param name="port">监听的端口</param>
        public bool StartListen(string ip, string port)
        {
            try
            {
                ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                ListenSocket.Bind(new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)));
                ListenSocket.Listen(Int32.MaxValue);
                isStop = false;
                StartAccept();
                return true;
            }
            catch (Exception ex)
            {
                string strException = string.Format("{0}发生系统异常[StartListen]。\r\n{1}\r\n\r\n\r\n", DateTime.Now, ex.Message + "(" + ex.StackTrace + ")");
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemException.log"), strException);
                return false;
            }
        }

        /// <summary>
        /// 停止监听，断开所有连接
        /// </summary>
        public void StopListen()
        {
            isStop = true;
            foreach (KeyValuePair<string, ClientOP> kv in dic_clients)
            {
                if (kv.Value.cSocket.Connected)
                    closeConnect(kv.Value.cSocket);
            }
            dic_clients.Clear();
            closeConnect(ListenSocket);
        }

        /// <summary>
        /// 线程，检查客户端离线
        /// </summary>
        private void Thread_Check_Logout()
        {
            try
            {
                while (true)
                {
                    ClientOP cp = new ClientOP();
                    foreach (KeyValuePair<string, ClientOP> kv in dic_clients)
                    {
                        int t = (DateTime.Now - kv.Value.Time).Seconds;
                        //SystemNote(kv.Key + ": " + kv.Value.TimeStr);
                        if (t > 60)
                        {
                            closeConnect(kv.Value.cSocket);
                            SystemNote("设备号: " + kv.Value.Devid + " 已断开连接");
                        }

                        if (!kv.Value.cSocket.Connected)//该客户端已经断开或者超过心跳时常
                        {
                            cp = new ClientOP();
                            cp.OpCode = 8;//标识为websocket退出包
                            cp.Devid = kv.Key;
                            removeConnectDic(cp);
                            que_msgs.Enqueue(cp);//通知离线消息
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// websocket数据包解析入口
        /// </summary>
        /// <param name="buffer">字节数组</param>
        /// <param name="len">长度</param>
        /// <param name="s">客户端的套接字</param>
        /// <returns>解析是否成功</returns>
        public bool Analyze(byte[] buffer, int len, ClientOP clientop)
        {
            try
            {
                Socket cSocket = clientop.cSocket;
                IPEndPoint ep = (IPEndPoint)cSocket.RemoteEndPoint;
                string ip = ep.Address.ToString();
                string port = ep.Port.ToString();
                string packetStr = Encoding.UTF8.GetString(buffer, 0, len);
                clientop.IP = ip;
                clientop.Port = port;
                if (Regex.Match(packetStr.ToLower(), "upgrade: websocket").Value == "")//当收到的数据[不是]握手包时
                {
                    if (Regex.Match(packetStr.ToLower(), "policy-file-request").Value == "")//当收到的数据[不是]flash请求策略文件时
                    {
                        clientop.Pac_Msg = AnalyzeClientData(clientop, buffer, len);//解析出客户端的消息
                        clientop.OpCode = clientop.Pac_Msg.Contains("ping") ? 10 : clientop.OpCode;//定义10表示心跳包
                    }
                }
                else
                {
                    cSocket.Send(AnswerHandShake(packetStr));//应答握手包
                    clientop.OpCode = 101;//连接成功的标识
                }
                if (dic_clients.ContainsKey(ip))//添加客户端
                    dic_clients[ip] = clientop;
                else
                {
                    dic_clients.TryAdd(ip, clientop);
                    IP.Add(ip);
                }
                return true;
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return false;
            }
        }
        /// <summary>
        /// 解析并处理DPS发过来的数据
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="cp"></param>
        public void pro_data(string msg, ClientOP cp)
        {
            //Send(cp.cSocket, msg);
            if (!msg.Contains("type"))
                return;
            //if (msg.Contains("meta")) IniHelper.WriteInfo(msg);
            try
            {
                JObject JO = (JObject)JsonConvert.DeserializeObject(msg);
                string type = JO["type"].ToString();
                switch (type)
                {
                    case "grade":
                        ProGradeInfo(JO, cp.cSocket); ;
                        break;
                    case "maskList":
                        ProMaskListInfo(JO, cp.cSocket);
                        break;
                    case "mask":
                        ProMaskInfo(JO, cp.cSocket);
                        break;
                    case "embroidery":
                        ProEmbroideryInfo(JO, cp.cSocket);
                        break;
                    case "embroideryList":
                        ProEmbroideryListInfo(JO, cp.cSocket);
                        break;
                    case "readCard":
                        ProReadCardInfo(JO, cp.cSocket);
                        break;
                    case "refresh":
                        ProRefreshInfo(JO, cp.cSocket);
                        break;
                    case "order":
                        ProOrderInfo(JO, cp.cSocket);
                        break;
                    case "styBomUpload":                      //款式物料模板
                        ProBoomTempletInfo(JO, cp.cSocket);
                        break;
                    case "styCraftUpload":                    //款式工艺模板
                        ProCraftTempletInfo(JO, cp.cSocket);
                        break;
                    case "lightSort":                         //灯光拣选
                        ProLightSortsInfo(JO, cp.cSocket);
                        break;
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }
        /// <summary>
        /// 应答客户端握手包
        /// </summary>
        /// <param name="packetStr">数据包字符串</param>
        /// <returns></returns>
        private byte[] AnswerHandShake(string packetStr)
        {
            string handShakeText = packetStr;
            string key = string.Empty;
            Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = reg.Match(handShakeText);
            if (m.Value != "")
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }

            byte[] secKeyBytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string secKey = Convert.ToBase64String(secKeyBytes);

            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKey + "\r\n\r\n");
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }

        /// <summary>
        /// 解析数据包
        /// </summary>
        /// <param name="buffer">数据包</param>
        /// <param name="len">长度</param>
        /// <returns></returns>
        private string AnalyzeClientData(ClientOP clientop, byte[] buffer, int len)
        {
            string str = Encoding.UTF8.GetString(buffer);
            bool mask = false;
            int lodlen = 0;
            if (len < 2)
                return string.Empty;
            clientop.Pac_Fin = (buffer[0] >> 7) > 0;
            if (!clientop.Pac_Fin)
                return string.Empty;
            clientop.OpCode = buffer[0] & 0xF;
            if (clientop.OpCode == 10)//心跳包(IE10及以上特有，不处理即可)
                return string.Empty;
            else if (clientop.OpCode == 8)//退出包
            {
                removeConnectDic(clientop);
                return string.Empty;
            }
            mask = (buffer[1] >> 7) > 0;
            lodlen = buffer[1] & 0x7F;
            byte[] loddata;
            byte[] masks = new byte[4];

            if (lodlen == 126)                  //0x7e
            {
                Array.Copy(buffer, 4, masks, 0, 4);
                lodlen = (UInt16)(buffer[2] << 8 | buffer[3]);
                loddata = new byte[lodlen];
                Array.Copy(buffer, 8, loddata, 0, lodlen);
            }
            else if (lodlen == 127)             //0x7f
            {
                Array.Copy(buffer, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = buffer[9 - i];
                }
                lodlen = (int)BitConverter.ToUInt64(uInt64Bytes, 0);

                loddata = new byte[lodlen];
                try
                {
                    for (int i = 0; i < lodlen; i++)
                    {
                        loddata[i] = buffer[i + 14];
                    }
                }
                catch { }
            }
            else
            {
                Array.Copy(buffer, 2, masks, 0, 4);
                loddata = new byte[lodlen];
                Array.Copy(buffer, 6, loddata, 0, lodlen);
            }
            for (var i = 0; i < lodlen; i++)
            {
                loddata[i] = (byte)(loddata[i] ^ masks[i % 4]);
            }
            return Encoding.UTF8.GetString(loddata);
        }

        /// <summary>
        /// 向客户端发送数据
        /// </summary>
        /// <param name="socket">客户端socket</param>
        /// <param name="message">要发送的数据</param>
        public static void Send(Socket socket, string message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                bool send = true;
                int SendMax = 65536;//每次分片最大64kb数据
                int count = 0;//发送的次数
                int sendedlen = 0;//已经发送的字节长度
                while (send)
                {
                    byte[] contentBytes = null;//待发送的消息内容
                    var sendArr = bytes.Skip(count * SendMax).Take(SendMax).ToArray();
                    sendedlen += sendArr.Length;
                    if (sendArr.Length > 0)
                    {
                        send = bytes.Length > sendedlen;//是否继续发送
                        if (sendArr.Length < 126)
                        {
                            contentBytes = new byte[sendArr.Length + 2];
                            contentBytes[0] = (byte)(count == 0 ? 0x81 : (!send ? 0x80 : 0));
                            contentBytes[1] = (byte)sendArr.Length;//1个字节存储真实长度
                            Array.Copy(sendArr, 0, contentBytes, 2, sendArr.Length);
                            send = false;
                        }
                        else if (sendArr.Length <= 65535)
                        {
                            contentBytes = new byte[sendArr.Length + 4];
                            if (!send && count == 0)
                                contentBytes[0] = 0x81;//非分片发送
                            else
                                contentBytes[0] = (byte)(count == 0 ? 0x01 : (!send ? 0x80 : 0));//处于连续的分片发送
                            contentBytes[1] = 126;
                            byte[] slen = BitConverter.GetBytes((short)sendArr.Length);//2个字节存储真实长度
                            contentBytes[2] = slen[1];
                            contentBytes[3] = slen[0];
                            Array.Copy(sendArr, 0, contentBytes, 4, sendArr.Length);
                        }
                        else if (sendArr.LongLength < long.MaxValue)
                        {
                            contentBytes = new byte[sendArr.Length + 10];
                            contentBytes[0] = (byte)(count == 0 ? 0x01 : (!send ? 0x80 : 0));//处于连续的分片发送
                            contentBytes[1] = 127;
                            byte[] llen = BitConverter.GetBytes((long)sendArr.Length);//8个字节存储真实长度
                            for (int i = 7; i >= 0; i--)
                            {
                                contentBytes[9 - i] = llen[i];
                            }
                            Array.Copy(sendArr, 0, contentBytes, 10, sendArr.Length);
                        }
                    }
                    socket.Send(contentBytes);
                    count++;
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
                //MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="so">连接的socket</param>
        private void closeConnect(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                socket.Close();
            }
            catch { }
        }

        /// <summary>
        /// 删除字典里的连接
        /// </summary>
        /// <param name="cp">客户端连接</param>
        private void removeConnectDic(ClientOP cp)
        {
            string ip = cp.IP;
            string port = cp.Port;
            string Devid = cp.Devid;
            ClientOP _cp = new ClientOP();
            if (dic_clients.ContainsKey(Devid))
            {
                if (dic_clients.TryRemove(Devid, out _cp))
                    closeConnect(cp.cSocket);
            }
        }
        /// <summary>
        /// 替换特殊字符
        /// </summary>
        /// <param name="str">替换的字符串</param>
        /// <returns></returns>
        private string replaceSpecStr(string str)
        {
            str = str.Replace(">", "&gt;");
            str = str.Replace("<", "&lt;");
            str = str.Replace(" ", "&nbsp;");
            str = str.Replace("\"", "&quot;");
            str = str.Replace("\'", "&#39;");
            str = str.Replace("\\", "\\\\");
            str = str.Replace("\n", "<br />");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "&emsp;");
            return str;
        }
        #endregion
        #region DPS相关方法
        public static void Dwon(string path, List<string> strList)
        {
            foreach (string str in strList)
            {
                DownFile(path, str);
            }
        }
        //下载文件
        private static void DownFile(string path, string adress)
        {
            try
            {
                string fileName = "";
                string filePath = "";
                string down_url = HttpUtility.UrlEncode(adress, Encoding.UTF8);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(adress);
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream responseStream = response.GetResponseStream();
                if (adress.Contains("filename"))                     //制版 排料文件下载
                {
                    string mathkey = "filename=";
                    fileName = adress.Substring(adress.LastIndexOf(mathkey)).Replace(mathkey, "");
                    filePath = path + @"\" + fileName;
                    if (fileName.Contains("自动排料")|| fileName.EndsWith("prj"))
                        filePath = filePath.Replace(@"result\", "");
                    else if (fileName.Contains("自动放码"))
                        filePath = filePath.Replace(@"sample\", "");
                }
                else                                                     //模板文件下载
                {
                    int n = adress.LastIndexOf("/") + 1;
                    fileName = adress.Substring(n);
                    filePath = path + @"\" + fileName;
                }
                Stream stream = new FileStream(filePath, FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    stream.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, bArr.Length);
                }
                stream.Close();
                responseStream.Close();
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
                if (ex.Message == "无法连接到远程服务器")
                    MessageBox.Show("下载地址无效");
            }
        }
        //上传文件
        public string UpLoad(UploadParam parameter)
        {
            try
            {
                //边界
                string boundary = string.Format("----{0}", DateTime.Now.Ticks.ToString("x"));
                string beginBoundary = string.Format("--{0}\r\n", boundary);
                string endBoundary = string.Format("\r\n--{0}--\r\n", boundary);
                byte[] beginBoundaryBytes = Encoding.UTF8.GetBytes(beginBoundary);
                byte[] endBoundaryBytes = Encoding.UTF8.GetBytes(endBoundary);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(parameter.Url);
                webRequest.Method = "POST";
                webRequest.ContentType = string.Format("multipart/form-data; boundary=" + boundary);
                using (Stream postStream = webRequest.GetRequestStream())
                {

                    postStream.Write(beginBoundaryBytes, 0, beginBoundary.Length);
                    //传字符参数
                    if (parameter.strParamters != null && parameter.strParamters.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> kv in parameter.strParamters)
                        {
                            string s = string.Format("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n{2}",
                                                     kv.Key, kv.Value, beginBoundary);
                            byte[] b = Encoding.Default.GetBytes(s);
                            postStream.Write(b, 0, b.Length);
                        }
                    }
                    //写入文件
                    for (int i = 0; i < parameter.FilesList.Count; i++)
                    {
                        if (i == parameter.FilesList.Count - 1)//最后一个文件
                        {
                            string filename = GetFileName(parameter.FilesList[i].FileName);
                            string str = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n",
                                           "files", filename);
                            byte[] b = Encoding.UTF8.GetBytes(str);
                            postStream.Write(b, 0, b.Length);
                            byte[] filedata = parameter.FilesList[i].FileData;
                            postStream.Write(filedata, 0, filedata.Length);
                            string s = string.Format("\r\n{0}", endBoundary);
                            byte[] bb = Encoding.UTF8.GetBytes(s);
                            postStream.Write(bb, 0, bb.Length);
                        }
                        else
                        {
                            string filename = GetFileName(parameter.FilesList[i].FileName);
                            WriteFiles("files", filename, parameter.FilesList[i].FileData, beginBoundary, postStream);
                        }
                    }
                    //string s = string.Format("\r\n{0}", endBoundary);
                    //byte[] bb = Encoding.UTF8.GetBytes(s);
                    //postStream.Write(bb, 0, bb.Length);
                    postStream.Close();
                    // .获取响应
                    using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            //IniHelper.WriteInfo(body);
                            if (body.Contains("data")) //正常数据
                            {
                                JObject o = (JObject)JsonConvert.DeserializeObject(body);
                                string addUploadFileStr = o["data"].ToString();
                                upLoadResult(parameter.businessId, addUploadFileStr, parameter.Type);
                            }
                            else if (body.Contains("message")) //异常数据
                            {
                                //if(dic_clients.ContainsKey(localIP))
                                //{
                                //    Send(dic_clients[localIP].cSocket, body);
                                //}
                            }
                            reader.Close();
                            return body;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                IniHelper.WriteLog(ex);
                return "";
            }
        }
        //第二次上传结果
        public void upLoadResult(string busisnessId, string addUploadFileStr,string type)
        {
            try
            {
                if (dic_clients.ContainsKey("127.0.0.1"))
                {
                    Result result = new Result();
                    result.meta = type;
                    result.data.addUploadFileStr = addUploadFileStr;
                    result.data.id = Form1.dic_id[busisnessId];
                    result.data.creator = Form1.dic_creator[busisnessId];
                    string msg = JsonConvert.SerializeObject(result);
                    Send(dic_clients["127.0.0.1"].cSocket, msg);
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        public static void LightSortReturn(int id,int qty)
        {
            try
            {
                if (dic_clients.ContainsKey("127.0.0.1"))
                {
                    LightReturn lr = new LightReturn();
                    lr.type = "lightSort";
                    lr.data.id = id;
                    lr.data.qty = qty;
                    string msg = JsonConvert.SerializeObject(lr);
                    Send(dic_clients["127.0.0.1"].cSocket, msg);
                    //IniHelper.WriteInfo(msg);
                }
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// 获取文件名称
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetFileName(string path)
        {
            string FileName = path.Substring(path.LastIndexOf(@"\") + 1);
            return FileName;
        }
        public void WriteFiles(string name, string fileName, byte[] fileData, string boundary, Stream postStream)
        {
            string str = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n",
                                       name, fileName);
            byte[] b = Encoding.UTF8.GetBytes(str);
            postStream.Write(b, 0, b.Length);
            postStream.Write(fileData, 0, fileData.Length);
            string s = string.Format("\r\n{0}", boundary);
            byte[] bb = Encoding.UTF8.GetBytes(s);
            postStream.Write(bb, 0, bb.Length);
        }
        //创建一个文件夹
        public void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                return;
                //DirectoryInfo di = new DirectoryInfo(path);
                //di.Delete(true);
                //Directory.CreateDirectory(path);
            }
        }
        //删除文件
        public void DeleteFile(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
            Directory.CreateDirectory(path);
        }
        public static void CopyFile(string sourcePath, string destPath)
        {
            string[] fileDir = Directory.GetFileSystemEntries(sourcePath);
            foreach (string str in fileDir)
            {
                string fileNmae = Path.GetFileName(str);
                if (fileNmae.Contains("."))
                {
                    File.Copy(str, destPath + "\\" + fileNmae, true);
                }
                else
                    return;
                //{
                //    if (!Directory.Exists(destPath + "\\" + fileNmae))
                //        Directory.CreateDirectory(destPath + "\\" + fileNmae);
                //    string[] sonfile = Directory.GetFileSystemEntries(str);
                //    CopyFile(str, destPath + "\\" + fileNmae);
                //}
            }
        }
        /// <summary>
        /// 处理制版信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProGradeInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "grade";
            string msg = "";
            try
            {
                businessId = JO["businessId"].ToString();
                businessId += "[" + DateTime.Now.ToString("HH_mm_ss") + "]";
                upload_file = JO["fdfsUploadUrl"].ToString();//上传文件URL
                string id = JO["id"].ToString();
                string creator = JO["creator"].ToString();
                Form1.dic_id.TryAdd(businessId, id);
                Form1.dic_creator.TryAdd(businessId, creator);
                string jsonStr = JO["jsonStr"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                gradeList.Clear();
                foreach (string str in ja)
                {
                    gradeList.Add(str);
                }
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
                //创建一个文件夹
                path = @"D:\ETCAD\制版\" + businessId;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                else return;
                //创建一个sample文件夹
                spath = @"D:\ETCAD\制版\" + businessId + "\\sample";
                if (!Directory.Exists(spath))
                    Directory.CreateDirectory(spath);
                else return;

            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理排料信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProMaskListInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "maskList";
            string msg = "";
            try
            {
                businessId = JO["businessId"].ToString();
                businessId += "[" + DateTime.Now.ToString("HH_mm_ss") + "]";
                upload_file = JO["fdfsUploadUrl"].ToString();//上传文件URL
                string id = JO["id"].ToString();
                string creator = JO["creator"].ToString();
                Form1.dic_id.TryAdd(businessId, id);
                Form1.dic_creator.TryAdd(businessId, creator);
                string jsonStr = JO["jsonStr"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                blowDown.Clear();
                foreach (string str in ja)
                {
                    blowDown.Add(str);
                }
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
                //创建一个文件夹
                path = @"D:\ETCAD\排料\" + businessId;
                CreateFolder(path);
                //创建sample文件夹
                spath = @"D:\ETCAD\排料\" + businessId + "\\result";
                CreateFolder(spath);
                //创建newpath文件夹
                newpath = @"D:\ETCAD\排料\" + businessId + "\\result2";
                CreateFolder(newpath);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理裁剪信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProMaskInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "mask";
            string msg = "";
            try
            {
                businessId = JO["businessId"].ToString();
                businessId += "[" + DateTime.Now.ToString("HH_mm_ss") + "]";
                string jsonStr = JO["jsonStr"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                maskList.Clear();
                foreach (string str in ja)
                {
                    maskList.Add(str);
                }
                //删除D盘下所有的.nc文件
                string[] filedir = Directory.GetFiles(@"D:\", "*.nc", SearchOption.TopDirectoryOnly);
                if (filedir != null)
                {
                    foreach (string fileName in filedir)
                    {
                        File.Delete(fileName);
                    }
                }
                //创建一个文件夹
                path = @"D:\ETCAD\裁剪\" + businessId;
                CreateFolder(path);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理绣花操作信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProEmbroideryInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "embroidery";
            string msg = "";
            try
            {
                businessId = JO["businessId"].ToString();
                businessId += "[" + DateTime.Now.ToString("HH_mm_ss") + "]";
                string jsonStr = JO["jsonStr"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                embOpera.Clear();
                foreach (string str in ja)
                {
                    embOpera.Add(str);
                }
                //创建一个文件夹
                path = @"D:\ETCAD\绣花操作\" + businessId;
                CreateFolder(path);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理绣花列表信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProEmbroideryListInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "embroideryList";
            string msg = "";
            try
            {
                businessId = JO["businessId"].ToString();
                businessId += "[" + DateTime.Now.ToString("HH_mm_ss") + "]";
                string jsonStr = JO["jsonStr"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                embList.Clear();
                foreach (string str in ja)
                {
                    embList.Add(str);
                }
                //创建一个文件夹
                path = @"D:\ETCAD\绣花列表\" + businessId;
                CreateFolder(path);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理读卡信息
        /// </summary>
        /// <param name="JO"></param>
        public void ProReadCardInfo(JObject JO, Socket socket)
        {
            ReadCardReturn rcr = new ReadCardReturn();
            rcr.type = "readCard";
            string msg = "";
            try
            {
                string po = JO["po"].ToString();                      //订单号  可能有多个
                string styNum = JO["styNum"].ToString();              //款号
                string qty = JO["qty"].ToString();                    //生产件数
                string lineNum= JO["lineNum"].ToString();
                string icNum = po + "(" + styNum + ")" + qty+"#"+lineNum;
                //JArray ja = (JArray)JsonConvert.DeserializeObject(poInfoList);
                cr.WriteData(icNum);
                icNum = cr.ReadCardID();
                rcr.data.po = po;
                rcr.data.icNum = icNum;
                msg = JsonConvert.SerializeObject(rcr);
                Send(socket, msg);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                WebReturn wr = new WebReturn();
                wr.type = "readCard";
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理刷新信息
        /// </summary>
        /// <param name="JO"></param>
        /// <param name="socket"></param>
        public void ProRefreshInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "refresh";
            string msg = "";
            try
            {
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
                refresh(1);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(msg);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理订单界面信息
        /// </summary>
        /// <param name="JO"></param>
        /// <param name="socket"></param>
        public void ProOrderInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "order";
            string msg = "";
            try
            {
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
                order(2);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(msg);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理款式物料模板信息
        /// </summary>
        /// <param name="JO"></param>
        /// <param name="socket"></param>
        public void ProBoomTempletInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "styBomUpload";
            string msg = "";
            try
            {
                string url = JO["url"].ToString();
                string path = @"D:\ETCAD\款式物料模板";
                wr.data = "success";
                DownFile(path, url);
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(msg);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 款式工艺模板信息
        /// </summary>
        /// <param name="JO"></param>
        /// <param name="socket"></param>
        public void ProCraftTempletInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "styCraftUpload";
            string msg = "";
            try
            {
                string url = JO["url"].ToString();
                string path = @"D:\ETCAD\款式工艺模板";
                DownFile(path, url);
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(msg);
                Send(socket, msg);
            }
        }
        /// <summary>
        /// 处理灯光拣选信息
        /// </summary>
        /// <param name="JO"></param>
        /// <param name="socket"></param>
        public void ProLightSortsInfo(JObject JO, Socket socket)
        {
            WebReturn wr = new WebReturn();
            wr.type = "styCraftUpload";
            string msg = "";
            try
            {
                string sortData = JO["sortData"].ToString();
                JArray ja = (JArray)JsonConvert.DeserializeObject(sortData);
                for(int i=0;i<ja.Count;i++)
                {
                    Light light = new Light();
                    double quantity= Convert.ToDouble(ja[i]["qty"]);
                    light.id  = Convert.ToInt32(ja[i]["id"]);
                    light.machineNum = Convert.ToInt32(ja[i]["machineNum"]);
                    light.qty = (int)Math.Ceiling(quantity);
                    Form1.que_light.Enqueue(light);
                }
                executeLigthSort();
                wr.data = "success";
                msg = JsonConvert.SerializeObject(wr);
                Send(socket, msg);
            }
            catch(Exception ex)
            {
                IniHelper.WriteLog(ex);
                wr.data = "failed";
                msg = JsonConvert.SerializeObject(msg);
                Send(socket, msg);
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace shell
{
    public partial class UpLoad : Form
    {
        DataTable dt1,dt2;
        public static ConcurrentDictionary<string, string> dic_order = new ConcurrentDictionary<string, string>();
        websocket mWebsocket;
        IniHelper mIni = new IniHelper();              
        public UpLoad(websocket mWebsocket)
        {
            InitializeComponent();
            dt1 = new DataTable();
            dt2 = new DataTable();
           this.mWebsocket = mWebsocket;
            mIni.ImportHelper(System.Environment.CurrentDirectory + "\\Param.ini");
        }
        private void UpLoad_Load(object sender, EventArgs e)
        {
            //制版
            List<string> gradeL = new List<string>(Form1.dic_gradeFiles.Keys);
            for(int i=0;i<gradeL.Count;i++)
            {
                bool flag = i == 0 ? true : false;
                checkedListBox1.Items.Add(gradeL[i], flag);
            }
            //排料
            List<string> maskListL = new List<string>(Form1.dic_maskListFiles.Keys);
            for (int i = 0; i < maskListL.Count; i++)
            {
                bool flag = i == 0 ? true : false;
                checkedListBox2.Items.Add(maskListL[i], flag);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string order_path = @"D:\ETCAD\制版\"; 
                foreach (object item in checkedListBox1.CheckedItems)
                {
                    string businessid = item.ToString();//选择的订单号
                    string search_path = @order_path + businessid + @"\result";
                    string[] filedic = Directory.GetFiles(search_path, "*.*", SearchOption.AllDirectories);//查找文件里里的所有文件
                    UploadParam uploadParam = new UploadParam();
                    uploadParam.Type = "grade";
                    uploadParam.businessId = businessid;
                    string businessType = "Grade";
                    uploadParam.strParamters.Add("businessType", businessType);
                    List<string> fileNameList = new List<string>();
                    foreach (string a in filedic)
                    {
                        fileNameList.Add(a);
                    }
                    AddParam(uploadParam, fileNameList);
                    mWebsocket.UpLoad(uploadParam);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                IniHelper.WriteLog(ex);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string order_path = @"D:\ETCAD\排料\";
                foreach (object item in checkedListBox2.CheckedItems)
                {
                    string businessid = item.ToString();//选择的订单号
                    string busid = businessid.Substring(0, businessid.IndexOf("["));
                    string search_path = @order_path + businessid + @"\result";
                    string[] filedic = Directory.GetFiles(search_path, "*.nc*", SearchOption.AllDirectories);//查找文件里里的所有文件
                    UploadParam uploadParam = new UploadParam();
                    uploadParam.Type = "maskList";
                    uploadParam.businessId = businessid;
                    string businessType = "MaskList";
                    uploadParam.strParamters.Add("businessType", businessType);
                    List<string> fileNameList = new List<string>();
                    foreach (string a in filedic)
                    {
                        //if (!a.Contains(@"result\"+ busid))
                            fileNameList.Add(a);
                    }
                    AddParam(uploadParam, fileNameList);
                    mWebsocket.UpLoad(uploadParam);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                IniHelper.WriteLog(ex);
            }
        }
        /// <summary>
        /// 添加上传的参数
        /// </summary>
        /// <param name="uploadParam"></param>
        /// <param name="fileNameList"></param>
        public void AddParam(UploadParam uploadParam, List<string> fileNameList)
        {
            uploadParam.Url = mWebsocket.upload_file;//上传文件URL
            string creator = Form1.dic_creator[uploadParam.businessId];
            string businessId = uploadParam.businessId;            
            uploadParam.strParamters.Add("creator", creator);
            uploadParam.strParamters.Add("businessId", businessId);
            for (int i = 0; i < fileNameList.Count; i++)
            {
                Files file = new Files();
                file.FileName = fileNameList[i];
                file.FileType = GetFileType(file.FileName);
                file.FileData = ReadFile(file.FileName);
                uploadParam.FilesList.Add(file);
            }
        }
        /// <summary>
        /// 根据文件路径读取文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public byte[] ReadFile(String fileName)
        {
            FileInfo fileinfo = new FileInfo(fileName);
            byte[] buf = new byte[fileinfo.Length];
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            fs.Read(buf, 0, buf.Length);
            fs.Close();
            return buf;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool check = checkBox1.Checked;
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, check);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bool check = checkBox2.Checked;
            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                checkedListBox2.SetItemChecked(i, check);
            }
        }

        public string GetFileType(string fileName)
        {
            if (Regex.Match(fileName.ToLower(), ".txt").Value != "")
                return "txt";
            else if (Regex.Match(fileName.ToLower(), ".prj").Value != "")
                return "prj";
            else if (Regex.Match(fileName.ToLower(), ".pod").Value != "")
                return "pod";
            else if (Regex.Match(fileName.ToLower(), ".emf").Value != "")
                return "emf";
            else
                return "";
        }  
    }
}

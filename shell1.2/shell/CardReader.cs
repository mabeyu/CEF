using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace shell
{
    class CardReader
    {
        //string udi = "00 00 00 00 00 00 00 00";
        //string showMessage = "";  //展示的数据
        public void WriteData(string data)
        {
            byte mode = 0;
            byte blk_add = 16;
            byte num_blk = 3;
            int n = 0;
            byte[] snr = new byte[6];
            snr = convertSNR("FF FF FF FF FF FF", 16);
            if (snr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
                return;
            }
            byte[] buff = new byte[16];
            data += @"*";
            buff = Encoding.UTF8.GetBytes(data);
            n = Reader.MF_Write(mode, blk_add, num_blk, snr, buff);
            if (n == 0)
            {
                byte[] buffer = new byte[1];
                int nRet = Reader.ControlBuzzer(30, 1, buffer);
                //MessageBox.Show("发卡成功");
            }

        }
        public string ReadCardID()
        {
            string str = "";
            byte[] snr = new byte[6];
            snr = convertSNR("FF FF FF FF FF FF", 6);
            if (snr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
            }

            byte[] buffer = new byte[16];

            int nRet = Reader.MF_Read(0, 16, 1, snr, buffer);
            string a = Encoding.UTF8.GetString(buffer);
            if (nRet != 0)
            {
                MessageBox.Show("读卡失败，请放正卡片");
            }
            else
            {
                str = showData(snr, 0, 4);
            }
            return str ;
        }
        //转换卡号专用
        private byte[] convertSNR(string str, int keyN)
        {
            string regex = "[^a-fA-F0-9]";
            string tmpJudge = Regex.Replace(str, regex, "");

            //长度不对，直接退回错误
            if (tmpJudge.Length != 12) return null;

            string[] tmpResult = Regex.Split(str, regex);
            byte[] result = new byte[keyN];
            int i = 0;
            foreach (string tmp in tmpResult)
            {
                result[i] = Convert.ToByte(tmp, 16);
                i++;
            }
            return result;
        }
        private string showData( byte[] data, int s, int e)
        {
            //非负转换
            for (int i = 0; i < e; i++)
            {
                if (data[s + i] < 0)
                    data[s + i] = Convert.ToByte(Convert.ToInt32(data[s + i]) + 256);
            }
            string str = "";
            //for (int i = s; i < e; i++)
            //{
            //    textResponse.Text += data[i].ToString("X2")+" ";
            //}
            //textResponse.Text += "\r\n";

            for (int i = 0; i < e; i++)
            {
                str += data[s + i].ToString("X2");
            }
            return str;

        }
        private string ProcStr(string str)
        {
            int n = str.Length % 16;
            string datastr = str;
            for (int i = 0; i < 16 - n; i++)
            {
                datastr += "*";
            }
            return datastr;
        }
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace shell
{
    public partial class SnapForm : Form
    {

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        public string strHeadImagePath; //打开图片的路径
        ImageCut1 IC1;
        Bitmap Bi;  //定义位图对像
        int x1;     //鼠标按下时横坐标
        int y1;     //鼠标按下时纵坐标
        int width;  //所打开的图像的宽
        int heigth; //所打开的图像的高
        bool HeadImageBool = false;    // 此布尔变量用来判断pictureBox1控件是否有图片
        Point p1;   //定义鼠标按下时的坐标点
        Point p2;   //定义移动鼠标时的坐标点
        Point p3;   //定义松开鼠标时的坐标点
        Image iniImg;
        public SnapForm(Image iniImg)
        {
            InitializeComponent();
            this.iniImg = iniImg;
        }

        private void SnapForm_Load(object sender, EventArgs e)
        {
            try
            {
                //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Opacity = 0.99;
                this.ShowIcon = false;
                //this.ShowInTaskbar = false;
                this.TopMost = true;
                this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                this.pictureBox1.Image = iniImg;
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
            }
        }
        public static void PrintScreen()
        {
            keybd_event((byte)0x2c, 0, (uint)0, UIntPtr.Zero);//down
            keybd_event((byte)0x2c, 0, (uint)2, UIntPtr.Zero);//up
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                this.Cursor = Cursors.Cross;
                this.p1 = new Point(e.X, e.Y);
                x1 = e.X;
                y1 = e.Y;
                if (this.pictureBox1.Image != null)
                {
                    HeadImageBool = true;
                }
                else
                {
                    HeadImageBool = false;
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (this.Cursor == Cursors.Cross)
                {
                    this.p2 = new Point(e.X, e.Y);
                    if ((p2.X - p1.X) > 0 && (p2.Y - p1.Y) > 0)     //当鼠标从左上角向开始移动时P3坐标
                    {
                        this.p3 = new Point(p1.X, p1.Y);
                    }
                    if ((p2.X - p1.X) < 0 && (p2.Y - p1.Y) > 0)     //当鼠标从右上角向左下方向开始移动时P3坐标
                    {
                        this.p3 = new Point(p2.X, p1.Y);
                    }
                    if ((p2.X - p1.X) > 0 && (p2.Y - p1.Y) < 0)     //当鼠标从左下角向上开始移动时P3坐标
                    {
                        this.p3 = new Point(p1.X, p2.Y);
                    }
                    if ((p2.X - p1.X) < 0 && (p2.Y - p1.Y) < 0)     //当鼠标从右下角向左方向上开始移动时P3坐标
                    {
                        this.p3 = new Point(p2.X, p2.Y);
                    }
                    this.pictureBox1.Invalidate();  //使控件的整个图面无效，并导致重绘控件
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (HeadImageBool)
                {
                    width = this.pictureBox1.Image.Width;
                    heigth = this.pictureBox1.Image.Height;
                    if ((e.X - x1) > 0 && (e.Y - y1) > 0)   //当鼠标从左上角向右下方向开始移动时发生
                    {
                        IC1 = new ImageCut1(x1, y1, Math.Abs(e.X - x1), Math.Abs(e.Y - y1));    //实例化ImageCut1类
                    }
                    if ((e.X - x1) < 0 && (e.Y - y1) > 0)   //当鼠标从右上角向左下方向开始移动时发生
                    {
                        IC1 = new ImageCut1(e.X, y1, Math.Abs(e.X - x1), Math.Abs(e.Y - y1));   //实例化ImageCut1类
                    }
                    if ((e.X - x1) > 0 && (e.Y - y1) < 0)   //当鼠标从左下角向右上方向开始移动时发生
                    {
                        IC1 = new ImageCut1(x1, e.Y, Math.Abs(e.X - x1), Math.Abs(e.Y - y1));   //实例化ImageCut1类
                    }
                    if ((e.X - x1) < 0 && (e.Y - y1) < 0)   //当鼠标从右下角向左上方向开始移动时发生
                    {
                        IC1 = new ImageCut1(e.X, e.Y, Math.Abs(e.X - x1), Math.Abs(e.Y - y1));      //实例化ImageCut1类
                    }
                    //this.pictureBox2.Width = (IC1.KiCut1((Bitmap)(this.pictureBox1.Image))).Width;
                    //this.pictureBox2.Height = (IC1.KiCut1((Bitmap)(this.pictureBox1.Image))).Height;
                    Image img = IC1.KiCut1((Bitmap)(this.pictureBox1.Image));
                    DialogResult dr = MessageBox.Show("是否保存截图", "截图完成", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                    if (dr == DialogResult.OK)
                    {
                        Clipboard.SetImage(img);
                        saveFileDialog1.Filter = "jpg|*.jpg|bmp|*.bmp|gif|*.gif";
                        string iniPath = Form1.dic_process[Form1.processId];
                        //MessageBox.Show(Form1.processId.ToString());
                        //MessageBox.Show(iniPath);
                        this.saveFileDialog1.InitialDirectory = @"D:\ETCAD\制版\" + iniPath + @"\result";
                        if (saveFileDialog1.ShowDialog() != DialogResult.Cancel)
                        {
                            Image image = Clipboard.GetImage();
                            image.Save(saveFileDialog1.FileName);
                        }
                        this.Close();
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        this.Refresh();
                    }
                    this.Cursor = Cursors.Default;
                }
                else
                {
                    this.Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }
        public Image GetSelectImage1(int Width, int Height)
        {
            Image initImage = this.pictureBox1.Image;
            //Image initImage = Bi;
            //原图宽高均小于模版，不作处理，直接保存 
            if (initImage.Width <= Width && initImage.Height <= Height)
            {
                //initImage.Save(fileSaveUrl, System.Drawing.Imaging.ImageFormat.Jpeg);
                return initImage;
            }
            else
            {
                //原始图片的宽、高 
                int initWidth = initImage.Width;
                int initHeight = initImage.Height;

                //非正方型先裁剪为正方型 
                if (initWidth != initHeight)
                {
                    //截图对象 
                    System.Drawing.Image pickedImage = null;
                    System.Drawing.Graphics pickedG = null;

                    //宽大于高的横图 
                    if (initWidth > initHeight)
                    {
                        //对象实例化 
                        pickedImage = new System.Drawing.Bitmap(initHeight, initHeight);
                        pickedG = System.Drawing.Graphics.FromImage(pickedImage);
                        //设置质量 
                        pickedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        pickedG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        //定位 
                        Rectangle fromR = new Rectangle((initWidth - initHeight) / 2, 0, initHeight, initHeight);
                        Rectangle toR = new Rectangle(0, 0, initHeight, initHeight);
                        //画图 
                        pickedG.DrawImage(initImage, toR, fromR, System.Drawing.GraphicsUnit.Pixel);
                        //重置宽 
                        initWidth = initHeight;
                    }
                    //高大于宽的竖图 
                    else
                    {
                        //对象实例化
                        pickedImage = new System.Drawing.Bitmap(initWidth, initWidth);
                        pickedG = System.Drawing.Graphics.FromImage(pickedImage);
                        //设置质量 
                        pickedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        pickedG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        //定位 
                        Rectangle fromR = new Rectangle(0, (initHeight - initWidth) / 2, initWidth, initWidth);
                        Rectangle toR = new Rectangle(0, 0, initWidth, initWidth);
                        //画图 
                        pickedG.DrawImage(initImage, toR, fromR, System.Drawing.GraphicsUnit.Pixel);
                        //重置高 
                        initHeight = initWidth;
                    }

                    initImage = (System.Drawing.Image)pickedImage.Clone();
                    //                //释放截图资源 
                    pickedG.Dispose();
                    pickedImage.Dispose();
                }

                return initImage;
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Pen pp = new Pen(Color.Red, 6);//画笔
                e.Graphics.DrawRectangle(pp, 0, 0, pictureBox1.Width - 6, pictureBox1.Height - 6);
                if (HeadImageBool)
                {
                    Pen p = new Pen(Color.Red, 2);//画笔
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    //Bitmap bitmap = new Bitmap(strHeadImagePath);
                    Bitmap bitmap = Bi;
                    Rectangle rect = new Rectangle(p3, new Size(System.Math.Abs(p2.X - p1.X), System.Math.Abs(p2.Y - p1.Y)));
                    e.Graphics.DrawRectangle(p, rect);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                IniHelper.WriteLog(ex);
                return;
            }
        }

        /// <summary>
        /// 按ESC键退出截图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SnapForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}

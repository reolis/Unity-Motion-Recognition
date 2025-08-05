using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

namespace Diplom1
{
    public partial class Form1 : Form
    {
        Capture capture;
        bool isFirstCamera = false, isSecondCamera = false;
        int brightness = 1;
        ConnectToUnity connector;
        bool isPose1 = false, isPose2 = false, isPose3 = false;
        public Detection detector = new Detection();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isFirstCamera)
            {
                capture = new Capture(0);
            }
            else if (isSecondCamera)
            {
                capture = new Capture(1);
            }

            connector.StartListening();
            //connector.StartBoneListener();

            timer1.Enabled = true;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Mat mat = new Mat();
            Bitmap bmp = capture.GetFrame();
            if (bmp != null)
            {
                mat = bmp.ToMat();

                detector.SendFrame(mat);
                detector.ReceiveLandmarks();
                detector.DrawLandmarks(mat);

                if (detector.CurrentLandmarks.Count == 0)
                    richTextBox2.Text = "Нет меток";
                else
                {
                    connector.SendBonePositions(detector.CurrentLandmarks);
                    richTextBox2.Text = $"Получено {detector.CurrentLandmarks.Count} меток";
                }

                bmp = BitmapConverter.ToBitmap(mat);
                pictureBox1.Image?.Dispose();
                pictureBox1.Image = (Bitmap)bmp.Clone();
                bmp.Dispose();
            }

            if (isPose1)
            {
                connector.SendUdpMessage("Pose 1");
                isPose1 = false;
            }
            if (isPose2)
            {
                connector.SendUdpMessage("Pose 2");
                isPose2 = false;
            }
            if (isPose3)
            {
                connector.SendUdpMessage("Pose 3");
                isPose3 = false;
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            isSecondCamera = radioButton2.Checked;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            brightness = trackBar1.Value;
            label6.Text = brightness.ToString();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            isPose1 = radioButton3.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            connector = new ConnectToUnity(this, pictureBox2);
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            isPose2 = radioButton4.Checked;
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            isPose3 = radioButton5.Checked;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            isFirstCamera = radioButton1.Checked;
        }

        
    }
}

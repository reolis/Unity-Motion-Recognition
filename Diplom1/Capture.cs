using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

namespace Diplom1
{
    internal class Capture
    {
        VideoCapture VideoCapture;
        Mat Frame;

        public Capture(int cameraNum)
        {
            VideoCapture = new VideoCapture(cameraNum);
            Frame = new Mat();
        }

        public Bitmap GetFrame()
        {
            if (VideoCapture == null || Frame == null)
                return null;

            VideoCapture.Read(Frame);
            if (!Frame.Empty())
                return BitmapConverter.ToBitmap(Frame);
            return null;
        }

        public void Release()
        {
            VideoCapture?.Release();
            VideoCapture?.Dispose();
            Frame?.Dispose();
        }
    }
}

using Newtonsoft.Json;
using OpenCvSharp;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Linq;
using System.Windows;

public class Detection
{
    UdpClient udpSend;
    UdpClient udpReceive;
    public List<HandLandmarkData> CurrentLandmarks = new List<HandLandmarkData>();

    public Detection()
    {
        udpSend = new UdpClient();
        udpReceive = new UdpClient(5052);
        udpReceive.Client.ReceiveTimeout = 10;
    }

    public void SendFrame(Mat frame)
    {
        Mat resized = new Mat();
        Cv2.Resize(frame, resized, new OpenCvSharp.Size(320, 240));

        var encodingParams = new ImageEncodingParam(ImwriteFlags.JpegQuality, 50);

        var buffer = resized.ToBytes(".jpg", encodingParams);

        udpSend.Send(buffer, buffer.Length, "127.0.0.1", 5055);

        resized.Dispose();
    }


    public void ReceiveLandmarks()
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpReceive.Receive(ref remoteEP);

            if (StartsWith(data, "JSON|"))
            {
                byte[] jsonData = new byte[data.Length - 5];
                Array.Copy(data, 5, jsonData, 0, data.Length - 5);

                string json = Encoding.UTF8.GetString(jsonData);

                var yourJsonSerializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                };

                var landmarks = JsonConvert.DeserializeObject<List<HandLandmarkData>>(json, yourJsonSerializerSettings);

                CurrentLandmarks = landmarks;

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при приёме: " + ex.Message);
        }
    }

    private bool StartsWith(byte[] data, string prefix)
    {
        byte[] p = Encoding.UTF8.GetBytes(prefix);
        if (data.Length < p.Length) return false;
        for (int i = 0; i < p.Length; i++)
            if (data[i] != p[i]) return false;
        return true;
    }

    public void DrawLandmarks(Mat frame)
    {
        foreach (var lm in CurrentLandmarks)
        {
            int cx = (int)(lm.x * frame.Width);
            int cy = (int)(lm.y * frame.Height);
            Scalar color = lm.type == "pose" ? Scalar.Green : Scalar.Magenta;
            Cv2.Circle(frame, new OpenCvSharp.Point(cx, cy), 5, color, -1);
        }
    }

    public class Landmark
    {
        public string type { get; set; }
        public int id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}

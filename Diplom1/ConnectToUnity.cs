using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static Detection;
using static Diplom1.HandTracker;

namespace Diplom1
{
    internal class ConnectToUnity
    {
        Form1 form;
        UdpClient senderClient = new UdpClient();
        UdpClient receiverClient;
        UdpClient boneReceiver;

        private PictureBox pictureBox;

        ArmAndHandsController armController = new ArmAndHandsController();

        private readonly Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Vector3> currentPositions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, Vector3> velocities = new Dictionary<string, Vector3>();

        private HandTracker leftHandTracker;
        private HandTracker rightHandTracker;

        public ConnectToUnity(Form1 form1, PictureBox pictureBox)
        {
            this.form = form1;
            this.pictureBox = pictureBox;

            var leftIds = new List<int> { 11, 13, 15 };
            var rightIds = new List<int> { 12, 14, 16};

            leftHandTracker = new HandTracker(HandType.Left, leftIds);
            rightHandTracker = new HandTracker(HandType.Right, rightIds);
        }

        public void SendUdpMessage(string message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                senderClient.Send(bytes, bytes.Length, "127.0.0.1", 7777);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отправки: " + ex.Message);
            }
        }

        public void SendArmRotations(Vector3 rightArm, Vector3 leftArm)
        {
            string message = $"rightArm:{rightArm.X:F2}={rightArm.Y:F2}={rightArm.Z:F2};leftArm:{leftArm.X:F2}={leftArm.Y:F2}={leftArm.Z:F2}";
            SendUdpMessage(message);
        }

        public void StartListening(int port = 9000)
        {
            receiverClient = new UdpClient(port);
            ThreadPool.QueueUserWorkItem(Listen);
        }

        private void Listen(object state)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    byte[] data = receiverClient.Receive(ref ep);

                    using (var ms = new MemoryStream(data))
                    {
                        Image img = Image.FromStream(ms);
                        pictureBox.Invoke((MethodInvoker)(() =>
                        {
                            pictureBox.Image?.Dispose();
                            pictureBox.Image = new Bitmap(img);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при приёме: " + ex.Message);
                }
            }
        }

        public void StartBoneListener(int port = 8888)
        {
            boneReceiver = new UdpClient(port);
            ThreadPool.QueueUserWorkItem(ListenBoneData);
        }

        private void ListenBoneData(object state)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    byte[] data = boneReceiver.Receive(ref ep);
                    string message = Encoding.UTF8.GetString(data);

                    if (message.StartsWith("JSON|"))
                    {
                        message = message.Substring(5);
                    }

                    var landmarks = JsonConvert.DeserializeObject<List<HandLandmarkData>>(message);

                    var leftHand = landmarks.Where(l => l.hand_label == "Left").ToList();
                    var rightHand = landmarks.Where(l => l.hand_label == "Right").ToList();

                    if (leftHand.Count > 0)
                    {
                        UpdateHandBones(leftHand, isLeft: true);
                    }

                    if (rightHand.Count > 0)
                    {
                        UpdateHandBones(rightHand, isLeft: false);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при приёме кости: " + ex.Message);
                }
            }
        }

        private void UpdateHandBones(List<HandLandmarkData> handLandmarks, bool isLeft)
        {
            var handTracker = isLeft ? leftHandTracker : rightHandTracker;

            foreach (var lm in handLandmarks)
            {
                Vector3 pos = new Vector3(lm.x, lm.y, lm.z);
                handTracker.UpdateBone(lm.id, pos, Quaternion.Identity, 1);
            }

            for (int id = 0; id < 21; id++)
            {
                Vector3 predictedPos = handTracker.PredictBonePosition(id, 0.5f);
                if (isLeft)
                    armController.SetLeftHandBonePosition(id, predictedPos);
                else
                    armController.SetRightHandBonePosition(id, predictedPos);
            }
        }

        public void SendBonePositions(List<HandLandmarkData> lm)
        {
            if (lm == null) return;

            float scale = 0.5f;
            var jsonList = new List<object>();

            void AddHandData(string label, int[] ids)
            {
                foreach (int id in ids)
                {
                    var lm0 = lm.FirstOrDefault(x => x.id == id && x.hand_label == label);

                    jsonList.Add(new
                    {
                        type = "hand",
                        hand_label = label,
                        id = id,
                        x = (lm0 != null ? lm0.x : 0) * scale,
                        y = (lm0 != null ? lm0.y : 0) * -scale,
                        z = (lm0 != null ? lm0.z : 0) * -scale,
                        valid = lm0 != null
                    });
                }
            }

            AddHandData("Left", new int[] { 11, 13, 15 });
            AddHandData("Right", new int[] { 12, 14, 16 });

            int[] torsoIds = { 0, 11, 12, 13, 14, 15, 16, 23, 24 };
            
            foreach (int id in torsoIds)
            {
                var lm0 = lm.FirstOrDefault(x => x.id == id);

                string label;
                switch (id)
                {
                    case 11:
                    case 13:
                    case 15:
                        label = "Left";
                        break;
                    case 12:
                    case 14:
                    case 16:
                        label = "Right";
                        break;
                    default:
                        label = "Center";
                        break;
                }

                jsonList.Add(new
                {
                    type = "pose",
                    id = id,
                    side = label,
                    x = (lm0 != null ? lm0.x : 0) * scale,
                    y = (lm0 != null ? lm0.y : 0) * -scale,
                    z = (lm0 != null ? lm0.z : 0) * -scale,
                    valid = lm0 != null
                });
            }

            try
            {
                string json = JsonConvert.SerializeObject(jsonList);
                string msg = "JSON|" + json;
                SendUdpMessage(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке JSON: " + ex.Message);
            }
        }
    }
}

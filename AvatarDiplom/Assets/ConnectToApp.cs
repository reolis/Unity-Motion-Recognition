using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System;
using System.Collections.Generic;

public class ConnectToApp : MonoBehaviour
{
    [Serializable]
    public class HandLandmarkData
    {
        public string type;
        public string hand_label;
        public string side;
        public int id;
        public float x, y, z;
    }

    [Serializable]
    private class Wrapper
    {
        public HandLandmarkData[] array;
    }

    public static List<HandLandmarkData> ParseJsonArray(string json)
    {
        string wrapped = "{\"array\":" + json + "}";
        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(wrapped);
        if (wrapper == null || wrapper.array == null) return new List<HandLandmarkData>();
        return new List<HandLandmarkData>(wrapper.array);
    }


    UdpClient udpClient;
    Thread receiveThread;
    public static ConcurrentQueue<BoneData> boneDataQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> leftHandQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> rightHandQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> leftPoseQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> rightPoseQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> centerPoseQueue = new ConcurrentQueue<BoneData>();
    public static ConcurrentQueue<BoneData> bodyPoseQueue = new ConcurrentQueue<BoneData>();

    void Start()
    {
        udpClient = new UdpClient(7777);
        receiveThread = new Thread(ReceiveData) { IsBackground = true };
        receiveThread.Start();
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message.StartsWith("JSON|"))
                {
                    string json = message.Substring(5);
                    List<HandLandmarkData> landmarks = ParseJsonArray(json);

                    foreach (var lm in landmarks)
                    {
                        Vector3 position = new Vector3(lm.x, lm.y, lm.z);

                        if (lm.type == "hand")
                        {
                            string boneName = MapHandLandmarkIdToBoneName(lm.hand_label, lm.id);
                            if (string.IsNullOrEmpty(boneName)) continue;

                            BoneData boneData = new BoneData(boneName, position, Quaternion.Euler(lm.x, lm.y, lm.z));

                            if (lm.hand_label == "Left")
                                leftHandQueue.Enqueue(boneData);
                            else if (lm.hand_label == "Right")
                                rightHandQueue.Enqueue(boneData);
                        }
                        else if (lm.type == "pose")
                        {
                            string boneName = MapPoseIdToBoneName(lm.id);
                            if (string.IsNullOrEmpty(boneName)) continue;

                            BoneData boneData = new BoneData(boneName, position, Quaternion.Euler(lm.x, lm.y, lm.z));

                            if (lm.side == "Left")
                                leftPoseQueue.Enqueue(boneData);
                            else if (lm.side == "Right")
                                rightPoseQueue.Enqueue(boneData);
                            else
                                bodyPoseQueue.Enqueue(boneData);
                        }
                        else
                        {
                            Debug.LogWarning($"Unknown type: {lm.type}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ReceiveData error: " + e.Message);
            }
        }
    }
    string MapHandLandmarkIdToBoneName(string handLabel, int id)
    {
        var leftMap = new Dictionary<int, string>()
        {
            {0, "B-hand.L"},
            {1, "B-thumb1.L"},
            {2, "B-thumb2.L"},
            {3, "B-thumb3.L"},
            {4, "B-thumbTip.L"},
            {5, "B-index1.L"},
            {6, "B-index2.L"},
            {7, "B-index3.L"},
            {8, "B-indexTip.L"},
            {9, "B-middle1.L"},
            {10, "B-middle2.L"},
            {11, "B-middle3.L"},
            {12, "B-middleTip.L"},
            {13, "B-ring1.L"},
            {14, "B-ring2.L"},
            {15, "B-ring3.L"},
            {16, "B-ringTip.L"},
            {17, "B-pinky1.L"},
            {18, "B-pinky2.L"},
            {19, "B-pinky3.L"},
            {20, "B-pinkyTip.L"},
        };

        var rightMap = new Dictionary<int, string>()
        {
            {0, "B-hand.R"},
            {1, "B-thumb1.R"},
            {2, "B-thumb2.R"},
            {3, "B-thumb3.R"},
            {4, "B-thumbTip.R"},
            {5, "B-index1.R"},
            {6, "B-index2.R"},
            {7, "B-index3.R"},
            {8, "B-indexTip.R"},
            {9, "B-middle1.R"},
            {10, "B-middle2.R"},
            {11, "B-middle3.R"},
            {12, "B-middleTip.R"},
            {13, "B-ring1.R"},
            {14, "B-ring2.R"},
            {15, "B-ring3.R"},
            {16, "B-ringTip.R"},
            {17, "B-pinky1.R"},
            {18, "B-pinky2.R"},
            {19, "B-pinky3.R"},
            {20, "B-pinkyTip.R"},
        };

        if (handLabel == "Left")
        {
            return leftMap.TryGetValue(id, out var boneName) ? boneName : null;
        }
        else if (handLabel == "Right")
        {
            return rightMap.TryGetValue(id, out var boneName) ? boneName : null;
        }
        return null;
    }

    string MapPoseIdToBoneName(int id)
    {
        var map = new Dictionary<int, string>()
        {
            {0, "B-head"},
            {11, "B-shoulder.L"},
            {12, "B-shoulder.R"},
            {13, "B-upperArm.L"},
            {14, "B-upperArm.R"},
            {15, "B-forearm.L"},
            {16, "B-forearm.R"},
            {23, "B-spine"},
            {24, "B-chest"},
        };

        return map.TryGetValue(id, out var boneName) ? boneName : null;
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        udpClient?.Close();
    }
}
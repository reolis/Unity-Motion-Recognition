using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class SendPictureUDP : MonoBehaviour
{
    public Animator animator;
    public Camera cam;
    public RenderTexture renderTexture;

    private UdpClient imageClient;
    private UdpClient dataClient;

    private IPEndPoint imageEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
    private IPEndPoint dataEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

    void Start()
    {
        imageClient = new UdpClient();
        dataClient = new UdpClient();

        InvokeRepeating(nameof(SendFrame), 0f, 0.1f);
        InvokeRepeating(nameof(SendArmRotation), 0f, 0.1f);
    }

    void SendFrame()
    {
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        cam.targetTexture = renderTexture;
        cam.Render();
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToJPG(40);

        if (bytes.Length < 50000)
        {
            imageClient.Send(bytes, bytes.Length, imageEndPoint);
        }
        else
        {
            Debug.LogWarning("Image too large for UDP packet. Try lower resolution or quality.");
        }

        Destroy(tex);
    }

    void SendArmRotation()
    {
        if (animator == null) return;

        Transform rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if (rightArm == null) return;

        Vector3 euler = rightArm.localRotation.eulerAngles;
        string message = $"rightArm:{euler.x:F2},{euler.y:F2},{euler.z:F2}";

        byte[] data = Encoding.UTF8.GetBytes(message);
        dataClient.Send(data, data.Length, dataEndPoint);
    }

    void OnApplicationQuit()
    {
        imageClient?.Close();
        dataClient?.Close();
    }
}

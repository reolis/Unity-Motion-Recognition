import socket
import cv2
import numpy as np
import mediapipe as mp
import json

recv_ip = '127.0.0.1'
recv_port = 5055
send_ip = '127.0.0.1'
send_port = 5052

recv_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
recv_sock.bind((recv_ip, recv_port))

send_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands
mp_pose = mp.solutions.pose

hands = mp_hands.Hands(static_image_mode=False, max_num_hands=2,
                       min_detection_confidence=0.5, min_tracking_confidence=0.5)

pose = mp_pose.Pose(static_image_mode=False,
                    model_complexity=1,
                    enable_segmentation=False,
                    min_detection_confidence=0.5,
                    min_tracking_confidence=0.5)

print(f"[INFO] UDP приёмник запущен на {recv_ip}:{recv_port}")
print(f"[INFO] UDP отправка будет производиться на {send_ip}:{send_port}")
print("[INFO] Система готова, ожидаем кадры...")

while True:
    try:
        data, addr = recv_sock.recvfrom(65536)

        np_data = np.frombuffer(data, dtype=np.uint8)
        frame = cv2.imdecode(np_data, cv2.IMREAD_COLOR)

        if frame is None:
            print("[WARN] Не удалось декодировать кадр.")
            continue

        orig_h, orig_w = frame.shape[:2]
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        coords = []
        
        hand_results = hands.process(rgb_frame)
        
        if hand_results.multi_hand_landmarks:
            print(f"[INFO] Обнаружено {len(hand_results.multi_hand_landmarks)} рук.")
            for hand_landmarks, handedness in zip(hand_results.multi_hand_landmarks, hand_results.multi_handedness):
                label = handedness.classification[0].label
                for id, lm in enumerate(hand_landmarks.landmark):
                    coords.append({
                        'type': 'hand',
                        'hand_label': label,
                        'id': id,
                        'x': lm.x,
                        'y': lm.y,
                        'z': lm.z
                    })

        pose_results = pose.process(rgb_frame)
        if pose_results.pose_landmarks:

            hand_and_torso_joint_ids = [
                11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22,
                23, 24,
                0,
                27
            ]

            for id in hand_and_torso_joint_ids:
                lm = pose_results.pose_landmarks.landmark[id]
                coords.append({'type': 'pose', 'id': id, 'x': lm.x, 'y': lm.y, 'z': lm.z})

        json_data = json.dumps(coords).encode('utf-8')
        json_packet = b'JSON|' + json_data

        if len(json_packet) < 65507:
            send_sock.sendto(json_packet, (send_ip, send_port))

    except Exception as e:
        print(f"[EXCEPTION] {e}")
        break

print("[INFO] Завершение работы...")
hands.close()
pose.close()
recv_sock.close()
send_sock.close()
cv2.destroyAllWindows()
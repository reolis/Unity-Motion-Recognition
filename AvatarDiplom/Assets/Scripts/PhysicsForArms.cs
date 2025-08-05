using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class PhysicsForArms
    {
        public static void SimulateHangingArm(Dictionary<string, Transform> targets, bool isLeft)
        {
            Vector3 shoulderDir = Vector3.up * 0.15f;
            Vector3 upperArmDir = Vector3.up * 0.25f;
            Vector3 forearmDir = Vector3.up * 0.2f;

            if (targets.TryGetValue(isLeft ? "B-upperArm.L" : "B-upperArm.R", out var upperArm) &&
                targets.TryGetValue(isLeft ? "B-forearm.L" : "B-forearm.R", out var forearm) &&
                targets.TryGetValue(isLeft ? "B-hand.L" : "B-hand.R", out var hand))
            {
                Vector3 upperArmPos = upperArm.position + shoulderDir;
                Vector3 forearmPos = upperArmPos + upperArmDir;
                Vector3 handPos = forearmPos + forearmDir;

                upperArm.position = Vector3.Lerp(upperArm.position, upperArmPos, Time.deltaTime * 2);
                forearm.position = Vector3.Lerp(forearm.position, forearmPos, Time.deltaTime * 2);
                hand.position = Vector3.Lerp(hand.position, handPos, Time.deltaTime * 2);

                upperArm.rotation = Quaternion.Lerp(upperArm.rotation,
                    Quaternion.LookRotation(forearm.position - upperArm.position, Vector3.back), Time.deltaTime * 2);

                forearm.rotation = Quaternion.Lerp(forearm.rotation,
                    Quaternion.LookRotation(hand.position - forearm.position, Vector3.back), Time.deltaTime * 2);
            }
        }

        public static void SimulateWithHandOnly(Dictionary<string, Transform> targets, bool isLeft)
        {
            if (targets.TryGetValue(isLeft ? "B-forearm.L" : "B-forearm.R", out var forearm) &&
                targets.TryGetValue(isLeft ? "B-hand.L" : "B-hand.R", out var hand))
            {
                Vector3 dirForearm = hand.position - forearm.position;
                dirForearm.z *= -1f;
                forearm.rotation = Quaternion.LookRotation(dirForearm, Vector3.back);
            }
        }

        public static void SimulateWithForearmOnly(Dictionary<string, Transform> targets, bool isLeft)
        {
            if (targets.TryGetValue(isLeft ? "B-upperArm.L" : "B-upperArm.R", out var upperArm) &&
                targets.TryGetValue(isLeft ? "B-forearm.L" : "B-forearm.R", out var forearm))
            {
                Vector3 dirUpper = forearm.position - upperArm.position;
                dirUpper.z *= -1f;
                upperArm.rotation = Quaternion.LookRotation(dirUpper, Vector3.back);
            }
        }
    }
}

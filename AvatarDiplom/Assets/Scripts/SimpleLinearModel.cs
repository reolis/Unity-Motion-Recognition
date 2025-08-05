using System.Collections.Generic;
using UnityEngine;

public class SimpleLinearModel
{
    public class BoneSample
    {
        public Vector3 Prev;
        public Vector3 Curr;
        public Vector3 Target;
    }

    private float[] weightsX = new float[6];
    private float[] weightsY = new float[6];
    private float[] weightsZ = new float[6];

    public void Train(List<BoneSample> samples, float learningRate = 0.01f, int epochs = 1000)
    {
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            foreach (var sample in samples)
            {
                float[] input = new float[]
                {
                    sample.Prev.x, sample.Prev.y, sample.Prev.z,
                    sample.Curr.x, sample.Curr.y, sample.Curr.z
                };

                float predX = Dot(weightsX, input);
                float predY = Dot(weightsY, input);
                float predZ = Dot(weightsZ, input);

                float errorX = predX - sample.Target.x;
                float errorY = predY - sample.Target.y;
                float errorZ = predZ - sample.Target.z;

                for (int i = 0; i < weightsX.Length; i++)
                {
                    weightsX[i] -= learningRate * errorX * input[i];
                    weightsY[i] -= learningRate * errorY * input[i];
                    weightsZ[i] -= learningRate * errorZ * input[i];
                }
            }
        }
    }

    public Vector3 Predict(Vector3 prev, Vector3 curr)
    {
        float[] input = new float[]
        {
            prev.x, prev.y, prev.z,
            curr.x, curr.y, curr.z
        };

        float x = Dot(weightsX, input);
        float y = Dot(weightsY, input);
        float z = Dot(weightsZ, input);

        return new Vector3(x, y, z);
    }

    private float Dot(float[] weights, float[] input)
    {
        float sum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += weights[i] * input[i];
        }
        return sum;
    }
}
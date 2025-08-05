using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Threading.Tasks;

public class PosePredictor : IDisposable
{
    private readonly int historyLength;
    private readonly MLContext mlContext;

    private readonly Dictionary<int, ITransformer> trainedModels = new Dictionary<int, ITransformer>();
    private readonly Dictionary<int, PredictionEngine<BonePositionData, BonePositionPrediction>> predictionEngines = new Dictionary<int, PredictionEngine<BonePositionData, BonePositionPrediction>>();
    private readonly Dictionary<int, List<BonePositionData>> trainingDataBuffers = new Dictionary<int, List<BonePositionData>>();
    private readonly Dictionary<int, FixedSizeQueue<TimedPosition>> positionHistory = new Dictionary<int, FixedSizeQueue<TimedPosition>>();
    private readonly object trainingLock = new object();
    private const int TrainBatchSize = 500;

    private readonly Dictionary<int, ITransformer> trainedRotationModels = new Dictionary<int, ITransformer>();
    private readonly Dictionary<int, PredictionEngine<BoneRotationData, BoneRotationPrediction>> rotationPredictionEngines = new Dictionary<int, PredictionEngine<BoneRotationData, BoneRotationPrediction>>();
    private readonly Dictionary<int, List<BoneRotationData>> rotationTrainingDataBuffers = new Dictionary<int, List<BoneRotationData>>();
    private readonly Dictionary<int, FixedSizeQueue<TimedRotation>> rotationHistory = new Dictionary<int, FixedSizeQueue<TimedRotation>>();
    private readonly object rotationTrainingLock = new object();
    private const int RotationTrainBatchSize = 500;

    private readonly float mlPredictionWeight;

    public PosePredictor(int historyLength = 5, float mlPredictionWeight = 0.3f)
    {
        this.historyLength = historyLength;
        this.mlPredictionWeight = mlPredictionWeight;
        mlContext = new MLContext();
    }

    public void UpdateBonePosition(int boneId, Vector3 position, float time)
    {
        if (!IsValid(position))
            return;

        if (!positionHistory.TryGetValue(boneId, out var historyQueue))
        {
            historyQueue = new FixedSizeQueue<TimedPosition>(historyLength + 2);
            positionHistory[boneId] = historyQueue;
        }

        historyQueue.Enqueue(new TimedPosition { Position = position, Time = time });

        if (historyQueue.Count == historyLength + 2)
        {
            var positions = historyQueue.ToArray();
            var newExample = CreateTrainingExample(positions);

            if (!trainingDataBuffers.TryGetValue(boneId, out var buffer))
            {
                buffer = new List<BonePositionData>(TrainBatchSize * 2);
                trainingDataBuffers[boneId] = buffer;
            }

            buffer.Add(newExample);

            if (buffer.Count >= TrainBatchSize)
            {
                var trainingDataCopy = new List<BonePositionData>(buffer);
                buffer.Clear();
                Task.Run(() => TrainModel(boneId, trainingDataCopy));
            }
        }
    }

    public void UpdateBoneRotation(int boneId, Quaternion rotation, float time)
    {
        if (!rotationHistory.TryGetValue(boneId, out var queue))
        {
            queue = new FixedSizeQueue<TimedRotation>(historyLength + 2);
            rotationHistory[boneId] = queue;
        }

        queue.Enqueue(new TimedRotation { Rotation = rotation, Time = time });

        if (queue.Count == historyLength + 2)
        {
            var rotations = queue.ToArray();
            var newExample = CreateRotationTrainingExample(rotations);

            if (!rotationTrainingDataBuffers.TryGetValue(boneId, out var buffer))
            {
                buffer = new List<BoneRotationData>(RotationTrainBatchSize * 2);
                rotationTrainingDataBuffers[boneId] = buffer;
            }

            buffer.Add(newExample);

            if (buffer.Count >= RotationTrainBatchSize)
            {
                var trainingDataCopy = new List<BoneRotationData>(buffer);
                buffer.Clear();
                Task.Run(() => TrainRotationModel(boneId, trainingDataCopy));
            }
        }
    }

    public Vector3 PredictPosition(int boneId, float deltaTime)
    {
        if (!positionHistory.TryGetValue(boneId, out var historyQueue) || historyQueue.Count < historyLength + 1)
            return Vector3.Zero;

        var positions = historyQueue.ToArray();
        Vector3 physicsPrediction = EstimateNextPositionUsingPhysics(positions, deltaTime);

        Vector3 mlPrediction = Vector3.Zero;
        if (predictionEngines.TryGetValue(boneId, out var predictionEngine))
        {
            var input = CreatePredictionInput(positions);
            lock (trainingLock)
            {
                mlPrediction = predictionEngine.Predict(input).ToVector3();
            }
        }

        return Vector3.Lerp(physicsPrediction, mlPrediction, mlPredictionWeight);
    }

    public Quaternion PredictRotation(int boneId, float deltaTime)
    {
        if (!rotationHistory.TryGetValue(boneId, out var queue) || queue.Count < historyLength + 1)
            return Quaternion.Identity;

        var rotations = queue.ToArray();
        Quaternion physicsPrediction = rotations[rotations.Length - 1].Rotation;

        Quaternion mlPrediction = physicsPrediction;

        if (rotationPredictionEngines.TryGetValue(boneId, out var predictionEngine))
        {
            var input = CreateRotationPredictionInput(rotations);
            lock (rotationTrainingLock)
            {
                var prediction = predictionEngine.Predict(input);
                mlPrediction = new Quaternion(prediction.NextQX, prediction.NextQY, prediction.NextQZ, prediction.NextQW);
                mlPrediction = Quaternion.Normalize(mlPrediction);
            }
        }

        return Quaternion.Slerp(physicsPrediction, mlPrediction, mlPredictionWeight);
    }

    private BonePositionData CreateTrainingExample(TimedPosition[] positions)
    {
        var speeds = new List<Vector3>();
        var accelerations = new List<Vector3>();

        for (int i = 1; i < positions.Length; i++)
        {
            float dt = positions[i].Time - positions[i - 1].Time;
            if (dt <= 0) dt = 0.01f;
            speeds.Add((positions[i].Position - positions[i - 1].Position) / dt);
        }

        for (int i = 1; i < speeds.Count; i++)
        {
            float dt = positions[i + 1].Time - positions[i].Time;
            if (dt <= 0) dt = 0.01f;
            accelerations.Add((speeds[i] - speeds[i - 1]) / dt);
        }

        if (speeds.Count < 3 || accelerations.Count < 2)
            throw new InvalidOperationException("Недостаточно данных для расчёта скорости и ускорения.");

        return new BonePositionData
        {
            P0X = positions[0].Position.X,
            P0Y = positions[0].Position.Y,
            P0Z = positions[0].Position.Z,
            V0X = speeds[0].X,
            V0Y = speeds[0].Y,
            V0Z = speeds[0].Z,
            A0X = accelerations[0].X,
            A0Y = accelerations[0].Y,
            A0Z = accelerations[0].Z,

            P1X = positions[1].Position.X,
            P1Y = positions[1].Position.Y,
            P1Z = positions[1].Position.Z,
            V1X = speeds[1].X,
            V1Y = speeds[1].Y,
            V1Z = speeds[1].Z,
            A1X = accelerations[1].X,
            A1Y = accelerations[1].Y,
            A1Z = accelerations[1].Z,

            P2X = positions[2].Position.X,
            P2Y = positions[2].Position.Y,
            P2Z = positions[2].Position.Z,
            V2X = speeds[2].X,
            V2Y = speeds[2].Y,
            V2Z = speeds[2].Z,
            A2X = accelerations[2].X,
            A2Y = accelerations[2].Y,
            A2Z = accelerations[2].Z,

            P3X = positions[3].Position.X,
            P3Y = positions[3].Position.Y,
            P3Z = positions[3].Position.Z,

            NextX = positions[positions.Length - 1].Position.X,
            NextY = positions[positions.Length - 1].Position.Y,
            NextZ = positions[positions.Length - 1].Position.Z
        };
    }

    private BonePositionData CreatePredictionInput(TimedPosition[] positions)
    {
        int len = positions.Length;
        if (len < 5)
            throw new InvalidOperationException("Недостаточно данных для предсказания.");

        Vector3 p0 = positions[len - 5].Position;
        Vector3 p1 = positions[len - 4].Position;
        Vector3 p2 = positions[len - 3].Position;
        Vector3 p3 = positions[len - 2].Position;

        float dt0 = positions[len - 4].Time - positions[len - 5].Time;
        float dt1 = positions[len - 3].Time - positions[len - 4].Time;
        float dt2 = positions[len - 2].Time - positions[len - 3].Time;

        dt0 = dt0 <= 0 ? 0.01f : dt0;
        dt1 = dt1 <= 0 ? 0.01f : dt1;
        dt2 = dt2 <= 0 ? 0.01f : dt2;

        Vector3 v0 = (p1 - p0) / dt0;
        Vector3 v1 = (p2 - p1) / dt1;
        Vector3 v2 = (p3 - p2) / dt2;

        Vector3 a0 = (v1 - v0) / dt1;
        Vector3 a1 = (v2 - v1) / dt2;

        return new BonePositionData
        {
            P0X = p0.X,
            P0Y = p0.Y,
            P0Z = p0.Z,
            V0X = v0.X,
            V0Y = v0.Y,
            V0Z = v0.Z,
            A0X = a0.X,
            A0Y = a0.Y,
            A0Z = a0.Z,

            P1X = p1.X,
            P1Y = p1.Y,
            P1Z = p1.Z,
            V1X = v1.X,
            V1Y = v1.Y,
            V1Z = v1.Z,
            A1X = a1.X,
            A1Y = a1.Y,
            A1Z = a1.Z,

            P2X = p2.X,
            P2Y = p2.Y,
            P2Z = p2.Z,

            P3X = p3.X,
            P3Y = p3.Y,
            P3Z = p3.Z
        };
    }

    private Vector3 EstimateNextPositionUsingPhysics(TimedPosition[] positions, float deltaTime)
    {
        int len = positions.Length;
        if (len < 4) return positions[len - 1].Position;

        Vector3 p0 = positions[len - 4].Position;
        Vector3 p1 = positions[len - 3].Position;
        Vector3 p2 = positions[len - 2].Position;
        Vector3 p3 = positions[len - 1].Position;

        float dt1 = positions[len - 3].Time - positions[len - 4].Time;
        float dt2 = positions[len - 2].Time - positions[len - 3].Time;
        float dt3 = positions[len - 1].Time - positions[len - 2].Time;

        dt1 = dt1 <= 0 ? 0.01f : dt1;
        dt2 = dt2 <= 0 ? 0.01f : dt2;
        dt3 = dt3 <= 0 ? 0.01f : dt3;

        Vector3 v1 = (p1 - p0) / dt1;
        Vector3 v2 = (p2 - p1) / dt2;
        Vector3 v3 = (p3 - p2) / dt3;

        Vector3 a1 = (v2 - v1) / dt2;
        Vector3 a2 = (v3 - v2) / dt3;

        Vector3 avgAcc = (a1 + a2) / 2f;
        Vector3 predicted = p3 + v3 * deltaTime + 0.5f * avgAcc * deltaTime * deltaTime;

        return predicted;
    }

    private BoneRotationData CreateRotationTrainingExample(TimedRotation[] rotations)
    {
        Quaternion q0 = rotations[0].Rotation;
        Quaternion q1 = rotations[1].Rotation;
        Quaternion q2 = rotations[2].Rotation;
        Quaternion q3 = rotations[3].Rotation;
        Quaternion qNext = rotations[rotations.Length - 1].Rotation;

        float dt0 = rotations[1].Time - rotations[0].Time;
        float dt1 = rotations[2].Time - rotations[1].Time;
        float dt2 = rotations[3].Time - rotations[2].Time;

        dt0 = dt0 <= 0 ? 0.01f : dt0;
        dt1 = dt1 <= 0 ? 0.01f : dt1;
        dt2 = dt2 <= 0 ? 0.01f : dt2;

        Vector3 angVel0 = GetAngularVelocity(q0, q1, dt0);
        Vector3 angVel1 = GetAngularVelocity(q1, q2, dt1);
        Vector3 angVel2 = GetAngularVelocity(q2, q3, dt2);

        Vector3 angAcc0 = (angVel1 - angVel0) / dt1;
        Vector3 angAcc1 = (angVel2 - angVel1) / dt2;

        return new BoneRotationData
        {
            Q0X = q0.X,
            Q0Y = q0.Y,
            Q0Z = q0.Z,
            Q0W = q0.W,
            AngularVelX = angVel0.X,
            AngularVelY = angVel0.Y,
            AngularVelZ = angVel0.Z,
            AngularAccX = angAcc0.X,
            AngularAccY = angAcc0.Y,
            AngularAccZ = angAcc0.Z,

            Q1X = q1.X,
            Q1Y = q1.Y,
            Q1Z = q1.Z,
            Q1W = q1.W,

            NextQX = qNext.X,
            NextQY = qNext.Y,
            NextQZ = qNext.Z,
            NextQW = qNext.W
        };
    }

    private BoneRotationData CreateRotationPredictionInput(TimedRotation[] rotations)
    {
        int len = rotations.Length;

        Quaternion q0 = rotations[len - 5].Rotation;
        Quaternion q1 = rotations[len - 4].Rotation;
        Quaternion q2 = rotations[len - 3].Rotation;
        Quaternion q3 = rotations[len - 2].Rotation;

        Vector3 angVel0 = GetAngularVelocity(q0, q1, rotations[len - 4].Time - rotations[len - 5].Time);
        Vector3 angVel1 = GetAngularVelocity(q1, q2, rotations[len - 3].Time - rotations[len - 4].Time);
        Vector3 angVel2 = GetAngularVelocity(q2, q3, rotations[len - 2].Time - rotations[len - 3].Time);

        Vector3 angAcc0 = (angVel1 - angVel0) / (rotations[len - 3].Time - rotations[len - 4].Time);
        Vector3 angAcc1 = (angVel2 - angVel1) / (rotations[len - 2].Time - rotations[len - 3].Time);

        return new BoneRotationData
        {
            Q0X = q0.X,
            Q0Y = q0.Y,
            Q0Z = q0.Z,
            Q0W = q0.W,
            AngularVelX = angVel0.X,
            AngularVelY = angVel0.Y,
            AngularVelZ = angVel0.Z,
            AngularAccX = angAcc0.X,
            AngularAccY = angAcc0.Y,
            AngularAccZ = angAcc0.Z,
            Q1X = q1.X,
            Q1Y = q1.Y,
            Q1Z = q1.Z,
            Q1W = q1.W
        };
    }

    private void TrainModel(int boneId, List<BonePositionData> trainingData)
    {
        if (trainingData == null || trainingData.Count == 0)
            return;

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Concatenate("Features",
            nameof(BonePositionData.P0X), nameof(BonePositionData.P0Y), nameof(BonePositionData.P0Z),
            nameof(BonePositionData.V0X), nameof(BonePositionData.V0Y), nameof(BonePositionData.V0Z),
            nameof(BonePositionData.A0X), nameof(BonePositionData.A0Y), nameof(BonePositionData.A0Z),

            nameof(BonePositionData.P1X), nameof(BonePositionData.P1Y), nameof(BonePositionData.P1Z),
            nameof(BonePositionData.V1X), nameof(BonePositionData.V1Y), nameof(BonePositionData.V1Z),
            nameof(BonePositionData.A1X), nameof(BonePositionData.A1Y), nameof(BonePositionData.A1Z),

            nameof(BonePositionData.P2X), nameof(BonePositionData.P2Y), nameof(BonePositionData.P2Z),
            nameof(BonePositionData.V2X), nameof(BonePositionData.V2Y), nameof(BonePositionData.V2Z),
            nameof(BonePositionData.A2X), nameof(BonePositionData.A2Y), nameof(BonePositionData.A2Z),

            nameof(BonePositionData.P3X), nameof(BonePositionData.P3Y), nameof(BonePositionData.P3Z)
            )
            .Append(mlContext.Regression.Trainers.Sdca());

        var model = pipeline.Fit(dataView);

        lock (trainingLock)
        {
            trainedModels[boneId] = model;
            predictionEngines[boneId] = mlContext.Model.CreatePredictionEngine<BonePositionData, BonePositionPrediction>(model);
        }

        Console.WriteLine($"Position model for bone {boneId} retrained on {trainingData.Count} samples.");
    }
    private void TrainRotationModel(int boneId, List<BoneRotationData> trainingData)
    {
        if (trainingData == null || trainingData.Count == 0)
            return;

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Concatenate("Features",
            nameof(BoneRotationData.Q0X), nameof(BoneRotationData.Q0Y), nameof(BoneRotationData.Q0Z), nameof(BoneRotationData.Q0W),
            nameof(BoneRotationData.AngularVelX), nameof(BoneRotationData.AngularVelY), nameof(BoneRotationData.AngularVelZ),
            nameof(BoneRotationData.AngularAccX), nameof(BoneRotationData.AngularAccY), nameof(BoneRotationData.AngularAccZ),
            nameof(BoneRotationData.Q1X), nameof(BoneRotationData.Q1Y), nameof(BoneRotationData.Q1Z), nameof(BoneRotationData.Q1W)
            )
            .Append(mlContext.Regression.Trainers.Sdca());

        var model = pipeline.Fit(dataView);

        lock (rotationTrainingLock)
        {
            trainedRotationModels[boneId] = model;
            rotationPredictionEngines[boneId] = mlContext.Model.CreatePredictionEngine<BoneRotationData, BoneRotationPrediction>(model);
        }

        Console.WriteLine($"Rotation model for bone {boneId} retrained on {trainingData.Count} samples.");
    }


    private bool IsValid(Vector3 pos)
    {
        return !(float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z));
    }

    private Vector3 GetAngularVelocity(Quaternion q1, Quaternion q2, float dt)
    {
        if (dt <= 0) dt = 0.01f;

        Quaternion dq = Quaternion.Multiply(q2, Quaternion.Inverse(q1));

        dq = Quaternion.Normalize(dq);

        dq.ToAxisAngle(out Vector3 axis, out float angle);

        if (angle > Math.PI) angle -= (float)(2 * Math.PI);

        return axis * (angle / dt);
    }

    public void Dispose()
    {
        foreach (var engine in predictionEngines.Values)
            engine?.Dispose();

        foreach (var engine in rotationPredictionEngines.Values)
            engine?.Dispose();
    }
}

public class FixedSizeQueue<T> : IEnumerable<T>
{
    private readonly Queue<T> queue = new Queue<T>();
    private readonly int maxSize;

    public FixedSizeQueue(int maxSize)
    {
        this.maxSize = maxSize;
    }

    public void Enqueue(T item)
    {
        queue.Enqueue(item);
        while (queue.Count > maxSize)
            queue.Dequeue();
    }

    public int Count => queue.Count;

    public T[] ToArray() => queue.ToArray();

    public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => queue.GetEnumerator();
}

public struct TimedPosition
{
    public Vector3 Position;
    public float Time;
}

public struct TimedRotation
{
    public Quaternion Rotation;
    public float Time;
}

public class BonePositionData
{
    public float P0X, P0Y, P0Z;
    public float V0X, V0Y, V0Z;
    public float A0X, A0Y, A0Z;

    public float P1X, P1Y, P1Z;
    public float V1X, V1Y, V1Z;
    public float A1X, A1Y, A1Z;

    public float P2X, P2Y, P2Z;
    public float V2X, V2Y, V2Z;
    public float A2X, A2Y, A2Z;

    public float P3X, P3Y, P3Z;

    public float NextX, NextY, NextZ;

    public Vector3 ToVector3() => new Vector3(NextX, NextY, NextZ);
}

public class BonePositionPrediction
{
    public float NextX;

    public float NextY;

    public float NextZ;

    public Vector3 ToVector3() => new Vector3(NextX, NextY, NextZ);
}

public class BoneRotationData
{
    public float Q0X, Q0Y, Q0Z, Q0W;

    public float AngularVelX, AngularVelY, AngularVelZ;
    public float AngularAccX, AngularAccY, AngularAccZ;

    public float Q1X, Q1Y, Q1Z, Q1W;

    public float NextQX, NextQY, NextQZ, NextQW;
}

public class BoneRotationPrediction
{
    [ColumnName("Score")]
    public float NextQX;

    [ColumnName("Score")]
    public float NextQY;

    [ColumnName("Score")]
    public float NextQZ;

    [ColumnName("Score")]
    public float NextQW;
}

public static class QuaternionExtensions
{
    public static void ToAxisAngle(this Quaternion q, out Vector3 axis, out float angle)
    {
        if (Math.Abs(q.W) > 1.0f)
            q = Quaternion.Normalize(q);

        angle = 2.0f * (float)Math.Acos(q.W);
        float den = (float)Math.Sqrt(1.0f - q.W * q.W);

        if (den > 0.0001f)
        {
            axis = new Vector3(q.X / den, q.Y / den, q.Z / den);
        }
        else
        {
            axis = new Vector3(1, 0, 0);
        }
    }
}
using System;
using Assets;
using UnityEngine;

namespace Assets
{
    public class KalmanFilter3D
    {
        const int StateSize = 6;  // [x, vx, y, vy, z, vz]
        const int MeasurementSize = 3;  // [x, y, z]

        private double[,] A;
        private double[,] H;
        private double[,] Q;
        private double[,] R;

        private double[,] P;
        private double[] x;

        public KalmanFilter3D(double accelerationNoise = 1.0, double measurementNoise = 0.1, double dt = 0.02)
        {
            A = GetTransitionMatrix(dt);
            H = GetMeasurementMatrix();
            Q = GetProcessNoise(accelerationNoise, dt);
            R = CreateDiagonalMatrix(MeasurementSize, measurementNoise);

            P = CreateDiagonalMatrix(StateSize, 1.0);
            x = new double[StateSize];
        }

        public void Predict()
        {
            x = MultiplyMatrixVector(A, x);
            P = AddMatrices(MultiplyMatrices(MultiplyMatrices(A, P), Transpose(A)), Q);
        }

        public void Update(Vector3 measurement)
        {
            double[] z = { measurement.x, measurement.y, measurement.z };

            double[] y = SubtractVectors(z, MultiplyMatrixVector(H, x));
            double[,] S = AddMatrices(MultiplyMatrices(MultiplyMatrices(H, P), Transpose(H)), R);
            double[,] K = MultiplyMatrices(MultiplyMatrices(P, Transpose(H)), Invert3x3Matrix(S));

            x = AddVectors(x, MultiplyMatrixVector(K, y));
            double[,] I = CreateIdentityMatrix(StateSize);
            P = MultiplyMatrices(SubtractMatrices(I, MultiplyMatrices(K, H)), P);
        }

        public Vector3 GetPosition()
        {
            return new Vector3((float)x[0], (float)x[2], (float)x[4]);
        }

        private static double[,] GetTransitionMatrix(double dt)
        {
            return new double[,]
            {
                {1, dt, 0,  0,  0,  0},
                {0,  1, 0,  0,  0,  0},
                {0,  0, 1, dt,  0,  0},
                {0,  0, 0,  1,  0,  0},
                {0,  0, 0,  0,  1, dt},
                {0,  0, 0,  0,  0,  1}
            };
        }

        private static double[,] GetMeasurementMatrix()
        {
            return new double[,]
            {
                {1, 0, 0, 0, 0, 0},
                {0, 0, 1, 0, 0, 0},
                {0, 0, 0, 0, 1, 0}
            };
        }

        private static double[,] GetProcessNoise(double accelerationNoise, double dt)
        {
            double[,] G = new double[6, 3];
            G[0, 0] = dt * dt / 2.0; G[1, 0] = dt;
            G[2, 1] = dt * dt / 2.0; G[3, 1] = dt;
            G[4, 2] = dt * dt / 2.0; G[5, 2] = dt;

            double[,] Qv = CreateDiagonalMatrix(3, accelerationNoise);
            return MultiplyMatrices(MultiplyMatrices(G, Qv), Transpose(G));
        }

        private static double[,] CreateDiagonalMatrix(int size, double value)
        {
            var m = new double[size, size];
            for (int i = 0; i < size; i++) m[i, i] = value;
            return m;
        }

        private static double[,] CreateIdentityMatrix(int size)
        {
            return CreateDiagonalMatrix(size, 1.0);
        }

        private static double[,] Transpose(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[cols, rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = matrix[i, j];
            return result;
        }

        private static double[] MultiplyMatrixVector(double[,] matrix, double[] vector)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new double[rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i] += matrix[i, j] * vector[j];
            return result;
        }

        private static double[,] MultiplyMatrices(double[,] A, double[,] B)
        {
            int aRows = A.GetLength(0), aCols = A.GetLength(1), bCols = B.GetLength(1);
            var result = new double[aRows, bCols];
            for (int i = 0; i < aRows; i++)
                for (int j = 0; j < bCols; j++)
                    for (int k = 0; k < aCols; k++)
                        result[i, j] += A[i, k] * B[k, j];
            return result;
        }

        private static double[,] AddMatrices(double[,] A, double[,] B)
        {
            int rows = A.GetLength(0), cols = A.GetLength(1);
            var result = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = A[i, j] + B[i, j];
            return result;
        }

        private static double[,] SubtractMatrices(double[,] A, double[,] B)
        {
            int rows = A.GetLength(0), cols = A.GetLength(1);
            var result = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = A[i, j] - B[i, j];
            return result;
        }

        private static double[] AddVectors(double[] A, double[] B)
        {
            var result = new double[A.Length];
            for (int i = 0; i < A.Length; i++)
                result[i] = A[i] + B[i];
            return result;
        }

        private static double[] SubtractVectors(double[] A, double[] B)
        {
            var result = new double[A.Length];
            for (int i = 0; i < A.Length; i++)
                result[i] = A[i] - B[i];
            return result;
        }

        private static double[,] Invert3x3Matrix(double[,] m)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];

            double A = (e * i - f * h);
            double B = -(d * i - f * g);
            double C = (d * h - e * g);
            double D = -(b * i - c * h);
            double E = (a * i - c * g);
            double F = -(a * h - b * g);
            double G = (b * f - c * e);
            double H = -(a * f - c * d);
            double I = (a * e - b * d);

            double det = a * A + b * B + c * C;
            if (Mathf.Abs((float)det) < 1e-8)
                return CreateIdentityMatrix(3);

            double invDet = 1.0 / det;

            return new double[,]
            {
                { A * invDet, D * invDet, G * invDet },
                { B * invDet, E * invDet, H * invDet },
                { C * invDet, F * invDet, I * invDet }
            };
        }
    }
}
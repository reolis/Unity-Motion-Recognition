using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;

namespace Diplom1
{
    internal class PredictorSaver
    {
        private readonly MLContext mlContext;

        public PredictorSaver(MLContext context)
        {
            mlContext = context;
        }

        public void SaveModel(ITransformer model, string path)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var emptyData = mlContext.Data.LoadFromEnumerable(new List<BonePositionData>());
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
            mlContext.Model.Save(model, emptyData.Schema, stream);
        }

        public ITransformer LoadModel(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Model file not found", path);

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var model = mlContext.Model.Load(stream, out _);
            return model;
        }
    }
    internal class BonePositionData
    {
        public float P0X, P0Y, P0Z;
        public float P1X, P1Y, P1Z;
        public float P2X, P2Y, P2Z;
        public float P3X, P3Y, P3Z;
        public float NextX, NextY, NextZ;
    }
}
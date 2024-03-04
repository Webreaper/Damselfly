using System;
using System.Collections.Generic;
using UMapx.Core;

namespace FaceEmbeddingsClassification;

/// <summary>
/// Defines the embeddings database.
/// </summary>
public class Embeddings
{
    /// <summary>
    /// Initializes the embeddings database.
    /// </summary>
    public Embeddings()
    {
        Vectors = new List<float[]>();
        Labels = new List<string>();
    }

    /// <summary>
    /// Initializes the embeddings database.
    /// </summary>
    /// <param name="vectors">Vectors</param>
    /// <param name="labels">Labels</param>
    public Embeddings(List<float[]> vectors, List<string> labels)
    {
        if (vectors.Count != labels.Count)
            throw new Exception("Input embedding vectors and labels must be same sizes.");

        Vectors = vectors;
        Labels = labels;
    }

    /// <summary>
    /// Adds embedding to embeddings database.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <param name="label">Label</param>
    public void Add(float[] vector, string label)
    {
        Vectors.Add(vector);
        Labels.Add(label);
    }

    /// <summary>
    /// Removes embedding from embeddings database.
    /// </summary>
    /// <param name="index">Index</param>
    public void Remove(int index)
    {
        Vectors.RemoveAt(index);
        Labels.RemoveAt(index);
    }

    /// <summary>
    /// Removes embedding from embeddings database.
    /// </summary>
    /// <param name="label">Label</param>
    public void Remove(string label)
    {
        int index = Labels.IndexOf(label);

        if (index != -1)
        {
            Remove(index);
        }
        else
        {
            throw new Exception("Embedding with selected label does not exist.");
        }
    }

    /// <summary>
    /// Clears embeddings database.
    /// </summary>
    public void Clear()
    {
        Vectors.Clear();
        Labels.Clear();
    }

    /// <summary>
    /// Returns embeddings database count.
    /// </summary>
    public int Count
    {
        get
        {
            return Vectors.Count;
        }
    }

    /// <summary>
    /// Gets or sets vectors.
    /// </summary>
    public List<float[]> Vectors { get; }

    /// <summary>
    /// Gets or sets labels.
    /// </summary>
    public List<string> Labels { get; }

    /// <summary>
    /// Score vector from database by Euclidean distance.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Label</returns>
    public (string, float) FromDistance(float[] vector)
    {
        var length = Count;
        var min = float.MaxValue;
        var index = -1;

        // do job
        for (var i = 0; i < length; i++)
        {
            var d = Vectors[i].Euclidean(vector);

            if (d < min)
            {
                index = i;
                min = d;
            }
        }

        // result
        var label = index != -1 ? Labels?[index] : string.Empty;
        return (label, min);
    }

    /// <summary>
    /// Score vector from database by cosine similarity.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Label and value</returns>
    public (string, float) FromSimilarity(float[] vector)
    {
        var length = Vectors.Count;
        var max = float.MinValue;
        var index = -1;

        // do job
        for (var i = 0; i < length; i++)
        {
            var d = Vectors[i].Cosine(vector);

            if (d > max)
            {
                index = i;
                max = d;
            }
        }

        // result
        var label = index != -1 ? Labels?[index] : string.Empty;
        return (label, max);
    }

}
using System;
using System.Collections.Generic;
using UMapx.Core;

namespace FaceEmbeddingsClassification;

/// <summary>
/// Defines the embeddings database.
/// </summary>
public class Embeddings
{
    private Dictionary<string, float[]> VectorLookup = new();

    public Embeddings()
    {
        
    }

/// <summary>
    /// Initializes the embeddings database.
    /// </summary>
    /// <param name="vectors">Vectors</param>
    /// <param name="labels">Labels</param>
    public Embeddings(Dictionary<string, float[]> vectorLookups)
    {
        VectorLookup = vectorLookups;
    }

    /// <summary>
    /// Adds embedding to embeddings database.
    /// </summary>
    /// <param name="label">Label</param>
    /// <param name="vector">Vector</param>
    public void Add(string label, float[] vector)
    {
        VectorLookup[label] = vector;
    }

    /// <summary>
    /// Removes embedding from embeddings database.
    /// </summary>
    /// <param name="label">Label</param>
    public void Remove(string label)
    {
        if( VectorLookup.ContainsKey(label) )
            VectorLookup.Remove(label);
    }

    /// <summary>
    /// Clears embeddings database.
    /// </summary>
    public void Clear()
    {
        VectorLookup.Clear();
    }

    /// <summary>
    /// Returns embeddings database count.
    /// </summary>
    public int Count
    {
        get
        {
            return VectorLookup.Count;
        }
    }


    /// <summary>
    /// Score vector from database by Euclidean distance.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Label</returns>
    public (string? personGuid, float similarity) FromDistance(float[] vector)
    {
        var min = float.MaxValue;
        string? closest = null;

        // do job
        foreach(var face in VectorLookup)
        {
            var d = face.Value.Euclidean(vector);

            if (d < min)
            {
                closest = face.Key;
                min = d;
            }
        }

        // result
        return (closest, min);
    }

    /// <summary>
    /// Score vector from database by cosine similarity.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Label and value</returns>
    public (string? personGuid, float similarity) FromSimilarity(float[] vector)
    {
        var max = float.MinValue;
        string? closest = null;

        // do job
        foreach( var face in VectorLookup)
        {
            var d = face.Value.Cosine(vector);

            if (d > max)
            {
                closest = face.Key;
                max = d;
            }
        }

        // result
        return (closest, max);
    }

}
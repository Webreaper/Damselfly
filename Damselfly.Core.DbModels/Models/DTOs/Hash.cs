using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models;

/// <summary>
/// Store hashes for an image.
/// </summary>
public class Hash
{
    [Key]
    public int HashId { get; set; }

    [Required]
    public virtual Image Image { get; set; }
    public int ImageId { get; set; }

    // The MD5 image hash. 
    public string MD5ImageHash { get; set; }

    // Four slices of the perceptual hash (split to allow
    // us to precalculate matches so we only have to calc
    // hamming distance on a subset of images.
    public string PerceptualHex1 { get; set; }
    public string PerceptualHex2 { get; set; }
    public string PerceptualHex3 { get; set; }
    public string PerceptualHex4 { get; set; }

    [NotMapped]
    public ulong PerceptualHashValue
    {
        get { return (ulong)Convert.ToInt64(PerceptualHash, 16); }
    }

    public double SimilarityTo(Hash other)
    {
        double similarity = HashExtensions.Similarity(PerceptualHashValue, other.PerceptualHashValue);

        Logging.LogVerbose($"Hash similarity {PerceptualHash} vs {other.PerceptualHash} = {similarity:P1} ({PerceptualHashValue} v {other.PerceptualHashValue})");

        return similarity;
    }

    /// <summary>
    /// Property accessor to set and get the sliced perceptual hash via a single Hex has string.
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public string PerceptualHash
    {
        get
        {
            return PerceptualHex1 + PerceptualHex2 + PerceptualHex3 + PerceptualHex4;
        }

        set
        {
            var fullHex = value.PadLeft(16, '0');

            var chunks = fullHex.Chunk(4).Select(x => new string(x)).ToArray();

            PerceptualHex1 = chunks[0];
            PerceptualHex2 = chunks[1];
            PerceptualHex3 = chunks[2];
            PerceptualHex4 = chunks[3];
        }
    }
}
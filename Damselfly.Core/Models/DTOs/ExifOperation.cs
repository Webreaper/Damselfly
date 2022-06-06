using System;
using System.ComponentModel.DataAnnotations;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.Models;

/// <summary>
/// Represents a pending operation to add or remove a keyword on an image
/// </summary>
public class ExifOperation
{
    public enum OperationType
    {
        Add,
        Remove
    };

    public enum ExifType
    {
        Keyword = 0,
        Caption = 1,
        Face = 2,
        Description = 3,
        Rating = 4,
        Copyright = 5
    };

    public enum FileWriteState
    {
        Pending = 0,
        Written = 1,
        Discarded = 9999,
        Failed = -1
    };

    [Key]
    public int ExifOperationId { get; set; }

    [Required]
    public virtual Image Image { get; set; }
    public int ImageId { get; set; }

    [Required]
    public string Text { get; set; }

    [Required]
    public ExifType Type { get; set; }

    [Required]
    public OperationType Operation { get; set; } = OperationType.Add;

    public DateTime TimeStamp { get; internal set; }
    public FileWriteState State { get; set; } = FileWriteState.Pending;

    public int? UserId { get; set; }
    public virtual AppIdentityUser User { get; set; }
}
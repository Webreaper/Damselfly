using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.Models;

/// <summary>
///     Represents a pending operation to add or remove a keyword on an image
/// </summary>
public class ExifOperation
{
    public enum ExifType
    {
        Keyword = 0,
        Caption = 1,
        Face = 2,
        Description = 3,
        Rating = 4,
        Copyright = 5,
        Rotate = 6 // Text == the rotation degrees - 90/180/270
    }

    public enum FileWriteState
    {
        Pending = 0,
        Written = 1,
        Discarded = 9999,
        Failed = -1
    }

    public enum OperationType
    {
        Add,
        Remove
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] 
    public int ExifOperationId { get; set; }

    [Required] public virtual Image Image { get; set; }

    public int ImageId { get; set; }

    [Required] public string Text { get; set; }

    [Required] public ExifType Type { get; set; }

    [Required] public OperationType Operation { get; set; } = OperationType.Add;

    // TODO: Should this setter be required/private?
    public DateTime TimeStamp { get; set; }
    public FileWriteState State { get; set; } = FileWriteState.Pending;

    public int? UserId { get; set; }
    public virtual AppIdentityUser User { get; set; }
}
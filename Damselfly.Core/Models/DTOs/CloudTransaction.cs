using System;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

/// <summary>
/// Transaction Count for Cloud services - to keep approximate track of usage
/// </summary>
public class CloudTransaction
{
    public enum TransactionType
    {
        Unknown = 0,
        AzureFace = 1
    };

    [Key]
    public int CloudTransactionId { get; set; }

    public TransactionType TransType { get; set; }
    public DateTime Date { get; set; }
    public int TransCount { get; set; }

    public override string ToString()
    {
        return $"{Date}: {TransCount}";
    }
}
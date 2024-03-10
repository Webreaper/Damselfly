using System;
using System.Collections.Generic;

namespace Damselfly.Core.Utils;

/// <summary>
///     Collection class representing a prioritised concurrent queue with uniqueness. For the items of type T
///     there are two functions - one to return the unique key for the item, the other to return the priority.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="K"></typeparam>
public class UniqueConcurrentPriorityQueue<T, K> where K : notnull
                                                 where T : class
{
    private readonly Func<T, K> _keyFunc;
    private readonly PriorityQueue<T, int> _queue = new();
    private readonly Dictionary<K, T> _queueLookup = new();

    public UniqueConcurrentPriorityQueue(Func<T, K> keyFunc)
    {
        _keyFunc = keyFunc;
    }

    public bool IsEmpty
    {
        get
        {
            lock ( _queue )
            {
                return _queueLookup.Count == 0;
            }
        }
    }

    /// <summary>
    ///     When we go to add, we first try and add to a dictionary, which guarantees uniqueness.
    /// </summary>
    /// <returns>Object of type T</returns>
    /// <exception cref="ApplicationException"></exception>
    public bool TryDequeue( out T? result)
    {
        result = default;

        lock ( _queue )
        {
            if ( _queue.TryDequeue(out var item, out var _) )
            {
                var key = _keyFunc(item);
                if ( !_queueLookup.Remove(key, out var _) )
                    // Something bad happened - the collections are now out of sync.
                    throw new ApplicationException($"Unable to remove key {key} from lookup.");

                result = item;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     When we dequeue, we remove the item from the lookup.
    /// </summary>
    /// <param name="newItem"></param>
    /// <param name="priority">Priority = lower means higher priority</param>
    /// <returns>True if the item was added successfully</returns>
    public bool TryAdd(T newItem, int priority)
    {
        var added = false;

        lock ( _queue )
        {
            var key = _keyFunc(newItem);

            Logging.LogVerbose($"Adding job with key '{key}' to work queue...");

            if ( _queueLookup.TryAdd(key, newItem) )
            {
                // Success - this means the item wasn't already in the collection. So enqueue it
                _queue.Enqueue(newItem, priority);
                added = true;
            }
        }

        return added;
    }
}
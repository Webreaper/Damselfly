using System;
using System.Collections.Concurrent;

namespace Damselfly.Core.Utils;

/// <summary>
/// Collection class representing a prioritised concurrent queue with uniqueness. For the items of type T
/// there are two functions - one to return the unique key for the item, the other to return the priority.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="K"></typeparam>
public class UniqueConcurrentPriorityQueue<T, K> where T : class
{
	private readonly ConcurrentPriorityQueue<T> _queue = new();
	private readonly ConcurrentDictionary<K, T> _queueLookup = new ConcurrentDictionary<K, T>();
	private readonly Func<T, K> _keyFunc;
	private readonly Func<T, int> _priorityFunc;

	public UniqueConcurrentPriorityQueue(Func<T, K> keyFunc, Func<T, int> priorityFunc)
	{
		_keyFunc = keyFunc;
		_priorityFunc = priorityFunc;
	}

	public bool IsEmpty => _queueLookup.IsEmpty;

	/// <summary>
	/// When we go to add, we first try and add to a dictionary, which guarantees uniqueness.
	/// </summary>
	/// <returns>Object of type T</returns>
	/// <exception cref="ApplicationException"></exception>
	public T TryDequeue()
	{
		T item = _queue.TryDequeue();

		if (item != null)
		{
			var key = _keyFunc(item);
			if (!_queueLookup.TryRemove(key, out var _))
			{
				// Something bad happened - the collections are now out of sync.
				throw new ApplicationException($"Unable to remove key {key} from lookup.");
			}
		}

		return item;
	}

	/// <summary>
	/// When we dequeue, we remove the item from the lookup.
	/// </summary>
	/// <param name="newItem"></param>
	/// <returns>True if the item was added successfully</returns>
	public bool TryAdd( T newItem )
	{
		bool added = false;
		var key = _keyFunc(newItem);

		if( _queueLookup.TryAdd( key, newItem ) )
        {
			// Success - this means the item wasn't already in the collection. So enqueue it
			_queue.Enqueue(newItem, _priorityFunc( newItem ));
			added = true;
        }

		return added;
	}
}

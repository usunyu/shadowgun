//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace LRUCache
{
	public class LRUCache<K, V>
	{
		public LRUCache(int capacity)
		{
			this.capacity = capacity;
		}

		// [MethodImpl(MethodImplOptions.Synchronized)]
		public bool get(K key, ref V retVal)
		{
			lock (this)
			{
				LinkedListNode<LRUCacheItem<K, V>> node;
				if (cacheMap.TryGetValue(key, out node))
				{
					//System.Console.WriteLine("Cache HIT " + key);
					V value = node.Value.value;

					lruList.Remove(node);
					lruList.AddLast(node);

					retVal = value;

					return true;
				}
				//System.Console.WriteLine("Cache MISS " + key);

				retVal = default(V);

				return false;
			}
		}

		//  [MethodImpl(MethodImplOptions.Synchronized)]
		public void add(K key, V val)
		{
			lock (this)
			{
				LinkedListNode<LRUCacheItem<K, V>> lnode;

				if (cacheMap.TryGetValue(key, out lnode))
				{
					lruList.Remove(lnode);
					cacheMap.Remove(key);
				}

				if (cacheMap.Count >= capacity)
				{
					removeFirst();
				}
				LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
				LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
				lruList.AddLast(node);
				cacheMap.Add(key, node);
			}
		}

		protected void removeFirst()
		{
			// Remove from LRUPriority
			LinkedListNode<LRUCacheItem<K, V>> node = lruList.First;
			lruList.RemoveFirst();
			// Remove from cache
			cacheMap.Remove(node.Value.key);
		}

		int capacity;
		Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
		LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();
	}

	class LRUCacheItem<K, V>
	{
		public LRUCacheItem(K k, V v)
		{
			key = k;
			value = v;
		}

		public K key;
		public V value;
	}
}

using UnityEngine;
using System.Collections.Generic;

// http://gamedevelopment.tutsplus.com/tutorials/shuffle-bags-making-random-feel-more-random--gamedev-1249
public class ShuffleBag<T> {
	private System.Random random = new System.Random();
	private List<T> data;

	private T currentItem;
	private int currentPosition = -1;
	
	private int Capacity { get { return data.Capacity; } }
	public int Size { get { return data.Count; } }
	
	public ShuffleBag(int initCapacity)
	{
		data = new List<T>(initCapacity);
	}

	public void Add(T item, int amount)
	{
		for (int i = 0; i < amount; i++)
			data.Add(item);
		
		currentPosition = Size - 1;
	}

	public T Next()
	{
		if (currentPosition < 1)
		{
			currentPosition = Size - 1;
			currentItem = data[0];
			
			return currentItem;
		}
		
		var pos = random.Next(currentPosition);
		
		currentItem = data[pos];
		data[pos] = data[currentPosition];
		data[currentPosition] = currentItem;
		currentPosition--;
		
		return currentItem;
	}

	public void PrintTest() {
		for (int i = 0; i < 5; i++) {
			string st = "";
			for (int j = 0; j < 25; j++) {
				st += Next() + ", ";
			}
			Debug.Log(st);
		}
	}
}

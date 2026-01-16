using System.Threading.Tasks;
using UnityEngine;

public abstract class GClass841
{
	public static async Task Await(this AsyncOperation operation)
	{
		while (!operation.isDone)
		{
			await Task.Yield();
		}
	}
}

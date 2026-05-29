using UnityEngine;

namespace MassEditor
{
	public class Cloner : MonoBehaviour
	{
		public static GameObject CloneGameObject(GameObject g) => Instantiate(g);
		
		public static void DestroyGameObject(GameObject g) => Destroy(g);
	}
}
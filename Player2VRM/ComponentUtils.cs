using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Player2VRM
{
	static class ComponentUtils
	{
		public static T GetComponentInParentRecursive<T>(this Component com) where T : Component
		{
			var trans = com.transform;
			while (trans)
			{
				var res = trans.GetComponent<T>();
				if (res != null) return res;
				trans = trans.parent;
			}

			return null;
		}

		public static T GetComponentInParentRecursive<T>(this GameObject go) where T : Component
		{
			var trans = go.transform;
			while (trans)
			{
				var res = trans.GetComponent<T>();
				if (res != null) return res;
				trans = trans.parent;
			}

			return null;
		}

		public static bool FindNameInParentRecursive(this Component com, string name)
		{
			var trans = com.transform;
			while (trans)
			{
				var res = trans.name;
				if (trans.name == name) return true;
				trans = trans.parent;
			}

			return false;
		}

		public static bool FindNameInParentRecursive(this GameObject go, string name)
		{
			var trans = go.transform;
			while (trans)
			{
				var res = trans.name;
				if (trans.name == name) return true;
				trans = trans.parent;
			}

			return false;
		}
	}
}

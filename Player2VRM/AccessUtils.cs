using HarmonyLib;
using Oc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player2VRM
{
	static class AccessUtils
	{
		public static ref Tout GetRefField<Tin, Tout>(this Tin self, string fieldName)
		{
			return ref AccessTools.FieldRefAccess<Tin, Tout>(self, fieldName);
		}

		public static Tout GetField<Tin, Tout>(this Tin self, string fieldName)
		{
			return (Tout)AccessTools.Field(typeof(Tout), fieldName).GetValue(self);
		}
	}
}

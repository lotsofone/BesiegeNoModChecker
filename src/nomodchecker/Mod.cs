using System;
using Modding;
using UnityEngine;

namespace NoModChecker
{
	public class Mod : ModEntryPoint
    {
        public static GameObject Instance;
        public override void OnLoad()
        {
            Mod.Instance = new GameObject("NoModChecker Mod");
            UnityEngine.Object.DontDestroyOnLoad(Mod.Instance);
            Mod.Instance.AddComponent<ModBehaviour>();
            // Called when the mod is loaded.
        }
	}
}

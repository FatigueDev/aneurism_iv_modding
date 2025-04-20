using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Entities.Player;
using Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace remove_hud
{

	public class RemoveHud_Component : MonoBehaviour
	{
		void Update()
		{
			// This shows a red squiggly, but the function and module _do_ exist.
			if(UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.N))
			{
				if(RemoveHud.storedVolumeProfileComponents != null)
				{
					// Get the player's current Fate reference.
					var currentFate = PlayerManager.LocalPlayerObject.GetComponent<PlayerFate>().CurrentFate;

					// RemoveHud.Log.LogInfo($"Toggling volume profile to be: {(RemoveHud.shouldRemoveHud ? "ON" : "OFF")}.");

					if(RemoveHud.shouldRemoveHud)
					{
						// Shallow copy BACK all the values we stored when the fate changed into the component.
						foreach(var component in RemoveHud.storedVolumeProfileComponents)
						{
							currentFate.volumeProfile.components.Add(component);
						}
					}
					else
					{
						currentFate.volumeProfile.components.Clear();
					}

					RemoveHud.shouldRemoveHud = !RemoveHud.shouldRemoveHud;
				}
				else
				{
					// RemoveHud.Log.LogWarning("Tried to input when the stored volume profile was null");
				}
			}
		}
	}

	[BepInPlugin("remove_hud", "Remove Hud", "0.0.1")]
	public class RemoveHud : BasePlugin
	{
		internal static new ManualLogSource Log;
		internal static bool shouldRemoveHud = true;
		internal static Il2CppSystem.Collections.Generic.List<VolumeComponent> storedVolumeProfileComponents = null;

		public override void Load()
		{
			Log = base.Log;

			// Add input to toggle the effects of the mod.
			// Adds at time of launch, which may cause issues.
			// Consider moving to ClearFateVolumeOnFateChanged when not exists somehow.
			AddComponent<RemoveHud_Component>();

			FateManager.add_OnClientPlayerFateChanged((FateManager.FateEvent)ClearFateVolumeOnFateChanged);

			Log.LogInfo($"Plugin remove_hud loaded successfully.");
		}

		public void ClearFateVolumeOnFateChanged(PlayerFate playerFate, Fate previousFate, Fate nextFate)
		{
			if(playerFate.isLocalPlayer && playerFate.isClient)
			{
				// Log.LogInfo("The player is both local and in client space. Checking next Fate...");
				if(nextFate is Fate and not null)
				{
					// Log.LogInfo("Clearing next fate's volume profile.");
					storedVolumeProfileComponents = new Il2CppSystem.Collections.Generic.List<VolumeComponent>();

					// Shallow copy the values of the volumeProfile to our stored buffer
					foreach(var component in nextFate.volumeProfile.components)
					{
						storedVolumeProfileComponents.Add(component);
					}

					if(shouldRemoveHud)
					{
						nextFate.volumeProfile.components.Clear();
					}
				}
			}
		}
	}
}
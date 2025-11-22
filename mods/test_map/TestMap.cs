using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace test_map
{
	[BepInPlugin("test_map", "A Test Map", "0.0.1")]
	[BepInDependency("custom_map_loader", BepInDependency.DependencyFlags.HardDependency)]
	public class TestMap : BasePlugin
	{
		public override void Load()
		{
			var pluginMetadata = MetadataHelper.GetMetadata(this);
			var assetsPath = Path.Join(BepInEx.Paths.PluginPath, pluginMetadata.GUID, "assets");
			var icon = custom_map_loader.CustomMapLoader.LoadIcon(Path.Join(assetsPath, "icon.png"));
			custom_map_loader.CustomMapLoader.RegisterAssetBundle(Path.Join(assetsPath, "scene_bundle"), icon);
		}
	}
}
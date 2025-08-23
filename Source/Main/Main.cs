using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

[assembly: AssemblyVersionAttribute("1.0.0.0")]
namespace SaveFileCompression;

public enum CompressionType
{
	None,
	Gzip,
	zstd
}

public partial class SaveFileCompression : Mod
{
	public static Settings settings = null!;
	public SaveFileCompression(ModContentPack content) : base(content)
	{
		Harmony harmony = new(id: "AmCh.SaveFileCompression");
		Patches.ScribeLoader_InitLoading.Patch(harmony);
		settings = GetSettings<Settings>();
		Patches.ScribeSaver_InitSaving.Patch(harmony);
		Patches.Dialog_FileList_DrawDateAndVersion.Patch(harmony);
	}
	public override void DoSettingsWindowContents(Rect inRect)
		=> settings!.DoSettingsWindowContents(inRect);
	public override string SettingsCategory()
		=> "SFC.Name".Translate();
}

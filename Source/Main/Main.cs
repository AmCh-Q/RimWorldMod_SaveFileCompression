using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

[assembly: AssemblyVersionAttribute("0.1.1.0")]

namespace SaveFileCompression;

public enum CompFormat
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
		settings = GetSettings<Settings>();
		Harmony harmony = new(id: "AmCh.SaveFileCompression");
		Patches.ScribeLoader_InitLoading.Patch(harmony);
		Patches.ScribeSaver_InitSaving.Patch(harmony);
		Patches.ScribeSaver_FinalizeSaving.Patch(harmony);
		Patches.Dialog_FileList_DrawDateAndVersion.Patch(harmony);
		Patches.Better_ModMismatch_Window.BeginReading.Patch(harmony);
		Patches.Better_ModMismatch_Window.EndReading.Patch(harmony);
	}

	public override void DoSettingsWindowContents(Rect inRect)
		=> settings!.DoSettingsWindowContents(inRect);

	public override string SettingsCategory()
		=> "SFC.Name".Translate();
}

using WindowsDebloat.Models;

namespace WindowsDebloat.Catalog;

public static class TweakCatalog
{
	public static readonly IReadOnlyList<CatalogItem> Items = new List<CatalogItem>
	{
		Item("telemetry", CatalogGroup.Tweak, true, "Disable telemetry & Compatibility Appraiser",
			"DiagTrack service, CompatTelRunner scheduled tasks (classic random 100% disk cause), CEIP tasks, telemetry policy to minimum."),
		Item("bingsearch", CatalogGroup.Tweak, true, "Disable Bing / web results in Start search",
			"Start menu search stops sending keystrokes to Bing and showing web junk."),
		Item("ads", CatalogGroup.Tweak, true, "Disable ads, suggestions & auto-installed promo apps",
			"Content Delivery Manager: sponsored auto-installs, Start/lock-screen/Settings tips and ads, advertising ID, 'finish setting up' nag."),
		Item("widgets", CatalogGroup.Tweak, true, "Disable Widgets / News and Interests",
			"Win10: taskbar news feed off. Win11: Widgets off + removes the Web Experience Pack (permanent WebView2 RAM hog)."),
		Item("copilot", CatalogGroup.Tweak, true, "Disable Copilot & Recall",
			"Copilot policy + taskbar button + app, Recall screen analysis (no-op on non-Copilot+ PCs)."),
		Item("gamedvr", CatalogGroup.Tweak, true, "Disable Game DVR background recording",
			"Xbox background game capture - measurable FPS cost even when unused."),
		Item("dop2p", CatalogGroup.Tweak, true, "Stop Delivery Optimization P2P upload",
			"Updates still download normally; your PC just stops seeding them to strangers."),
		Item("edge", CatalogGroup.Tweak, true, "Stop Edge running in the background",
			"Startup Boost, background mode and sidebar off - no Edge processes when Edge is closed."),
		Item("activity", CatalogGroup.Tweak, true, "Disable activity history upload",
			"Stops publishing/uploading your timeline activity to Microsoft."),
		Item("sysmain", CatalogGroup.Tweak, true, "Disable SysMain (Superfetch)",
			"The #1 '100% disk' culprit on HDDs; pointless on SSDs."),

		Item("onedrive", CatalogGroup.Advanced, false, "Uninstall OneDrive",
			"Full uninstall. Local files in your OneDrive folder are kept, but sync stops."),
		Item("xboxsvc", CatalogGroup.Advanced, false, "Disable Xbox services",
			"XblAuthManager, XblGameSave, XboxNetApiSvc, XboxGipSvc. Breaks Game Pass and Xbox/Minecraft sign-in."),
		Item("indexing", CatalogGroup.Advanced, false, "Disable Windows Search indexing",
			"Stops background disk churn, but file search in Start/Explorer becomes slow."),
		Item("bgapps", CatalogGroup.Advanced, false, "Block background Store apps (Win10 only)",
			"Global off-switch. Store-app notifications and alarms may stop working."),
		Item("visualfx", CatalogGroup.Advanced, false, "Visual effects to best performance",
			"Animations and transparency off - snappier on weak hardware, plainer look."),
		Item("hibernate", CatalogGroup.Advanced, false, "Disable hibernation & Fast Startup",
			"Frees disk (~40% of RAM size). Removes Hibernate from power options."),
	};

	static CatalogItem Item(string id, CatalogGroup group, bool defaultOn, string title, string desc)
	{
		return new CatalogItem
		{
			Id = id,
			Title = title,
			Desc = desc,
			Default = defaultOn,
			Group = group
		};
	}
}

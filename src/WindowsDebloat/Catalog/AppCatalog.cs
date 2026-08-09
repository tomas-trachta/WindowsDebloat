using WindowsDebloat.Models;

namespace WindowsDebloat.Catalog;

public static class AppCatalog
{
	public static readonly IReadOnlyList<CatalogItem> Items = new List<CatalogItem>
	{
		Item("cortana", "Cortana", "Deprecated voice assistant.", true, "Microsoft.549981C3F5F10"),
		Item("news", "Microsoft News", "Bing news feed app.", true, "Microsoft.BingNews"),
		Item("weather", "Weather", "MSN Weather app.", true, "Microsoft.BingWeather"),
		Item("gethelp", "Get Help", "Microsoft support chat app.", true, "Microsoft.GetHelp"),
		Item("tips", "Tips", "Windows tips / 'Get Started' ads.", true, "Microsoft.Getstarted"),
		Item("3dviewer", "3D Viewer", "3D model viewer almost nobody uses.", true, "Microsoft.Microsoft3DViewer"),
		Item("officehub", "Office / Microsoft 365 hub", "Advertisement app for Microsoft 365 - not Office itself.", true, "Microsoft.MicrosoftOfficeHub"),
		Item("solitaire", "Solitaire Collection", "Ad-supported card games.", true, "Microsoft.MicrosoftSolitaireCollection"),
		Item("mixedreal", "Mixed Reality Portal", "VR portal for headsets you probably don't own.", true, "Microsoft.MixedReality.Portal"),
		Item("people", "People", "Contacts hub tied to Mail/Calendar.", true, "Microsoft.People"),
		Item("skype", "Skype", "Preinstalled Skype (deprecated by Microsoft).", true, "Microsoft.SkypeApp"),
		Item("todo", "Microsoft To Do", "Task list app.", true, "Microsoft.Todos"),
		Item("feedback", "Feedback Hub", "Bug reporting portal for Windows insiders.", true, "Microsoft.WindowsFeedbackHub"),
		Item("maps", "Maps", "Offline maps app (uses a background sync task).", true, "Microsoft.WindowsMaps"),
		Item("phonelink", "Phone Link", "Android/iPhone companion. Keep if you mirror your phone.", true, "Microsoft.YourPhone"),
		Item("groove", "Groove Music / Media Player", "Store music player.", true, "Microsoft.ZuneMusic"),
		Item("movies", "Movies & TV", "Store video player / movie shop.", true, "Microsoft.ZuneVideo"),
		Item("powerauto", "Power Automate", "RPA automation tool preinstalled on Win11.", true, "Microsoft.PowerAutomateDesktop"),
		Item("devhome", "Dev Home", "Developer dashboard preinstalled on Win11.", true, "Microsoft.Windows.DevHome"),
		Item("newoutlook", "Outlook (new)", "Ad-supported new Outlook client.", true, "Microsoft.OutlookForWindows"),
		Item("teams", "Teams / Chat", "Consumer Teams + Win11 taskbar 'Chat'.", true, "MicrosoftTeams", "MSTeams"),
		Item("clipchamp", "Clipchamp", "Video editor preinstalled on Win11.", true, "Clipchamp.Clipchamp"),
		Item("xboxapps", "Xbox apps & Game Bar", "Xbox app, Game Bar overlay and companions. Keep if you use Game Pass / Win+G.", true,
			"Microsoft.GamingApp", "Microsoft.XboxApp", "Microsoft.XboxGamingOverlay", "Microsoft.XboxGameOverlay", "Microsoft.Xbox.TCUI"),
		Item("promogames", "Candy Crush / promo games", "Third-party games Windows installed without asking.", true, "king.com.*"),
		Item("mail", "Mail & Calendar", "Classic Mail/Calendar apps. OFF by default - keep them if you use them.", false, "Microsoft.windowscommunicationsapps"),
		Item("onenote", "OneNote (Store version)", "Store OneNote. OFF by default - keep if you take notes with it.", false, "Microsoft.Office.OneNote"),
		Item("xboxident", "Xbox Identity Provider", "WARNING: needed for Minecraft and Xbox sign-in. Remove only if sure.", false, "Microsoft.XboxIdentityProvider"),
	};

	static CatalogItem Item(string id, string title, string desc, bool defaultOn, params string[] packages)
	{
		return new CatalogItem
		{
			Id = id,
			Title = title,
			Desc = desc,
			Default = defaultOn,
			Group = CatalogGroup.Apps,
			Packages = packages
		};
	}
}

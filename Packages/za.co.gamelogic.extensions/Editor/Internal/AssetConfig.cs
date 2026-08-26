using System;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Configuration data for the Gamelogic asset tools and windows.
	/// </summary>
	/*	Design note: Public so any asset can use it.
	*/
	[Serializable] // So windows remain good even when scripts recompile
	public class AssetConfig
	{
		/// <summary>The display name of the asset shown in windows and labels.</summary>
		public string assetDisplayName;
		/// <summary>The Unity package ID of this asset.</summary>
		public string packageId;
		/// <summary>The current version of the package.</summary>
		public string packageVersion;
		/// <summary>The URL to the documentation for this asset.</summary>
		public string documentationUrl;
		/// <summary>The URL to the YouTube channel for this asset.</summary>
		public string youTubeChannelUrl;

		/// <summary>The PlayerPrefs key used to track whether the welcome window has been shown.</summary>
		public string shownKey;

		/// <summary>
		/// All packages belonging to this asset. The first entry is treated as the main package
		/// (used for sample imports).
		/// </summary>
		public Package[] uninstallList;
	}
}

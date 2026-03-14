namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class Structuring
	{
		public static bool CheckStructure()
		{
			// 1, 2, 4, 8, 16, 32 - higher number, more extreme
			int infractions = 0;
			char separator = File.;
			if (!Directory.Exists(GetFullPath("config/"))) infractions += 32;
			if (!Directory.Exists(GetFullPath("assets/"))) infractions += 16;
			if (!Directory.Exists(GetFullPath("fonts/"))) infractions += 8;
			if (!Directory.Exists(GetFullPath("Card Data/"))) infractions += 7;
			else
			{
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-TextFiles/")))) infractions += 4;
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-Layout/")))) infractions += 2;
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-Images/")))) infractions += 1;
			}
			switch (infractions)
			{
				case 63:
					Console.WriteLine("Executable is not in project root. Please relocate to Cross-Platform-Card-Generator folder.");
					return false;
				case int n when n < 63 && n >= 8:
					Console.WriteLine("Missing one or more vital configuration folders. Please redownload or relocate missing folders to the Cross-Platform-Card-Generator folder.");
					return false;
				case 7:
					Console.WriteLine("Missing Card Data folder. Please redownload or relocate the folder to the Cross-Platform-Card-Generator folder.");
					return false;
				case int n when n < 7 && n >= 1:
					Console.WriteLine("Missing one or more vital Card Data folders. Please ensure -TextFiles, -Layout, and -Images are placed in Card Data.");
					return false;
				case 0:
					return true;
				default:
					return false;
			}
		}

		public static string GetFullPath(string relativePath)
		{
			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			bool debugging = Directory.GetParent(baseDir).Name == "net10.0";
			if (debugging) return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", relativePath));
			else return Path.GetFullPath(Path.Combine(baseDir, relativePath));
		}

		public static bool ElementExists(string deck, string element)
		{
			string relativePath = Path.Combine("Card Data", "-Layout", deck + element + ".png");
			if (!File.Exists(GetFullPath(relativePath)))
			{
				relativePath = deck + element + ".jpg";
				if (!File.Exists(GetFullPath(relativePath)))
				{
					relativePath = deck + element + ".jpeg";
					return File.Exists(GetFullPath(relativePath));
				}
				return true;
			}
			return true;
		}

		public static bool AssetExists(string assetCode)
		{
			string assetName;
			assetName = TextManipulation.GetAssetName(assetCode);
			if (assetName == "") return false;
			if (TextManipulation.GainPowerAmount(assetName) != "")
			{
				assetName = "GainPower";
			}
			string pathNoExt = Path.Combine("assets", assetName);
			string relativePath = pathNoExt + FindExtension("assets", assetName);
			return File.Exists(GetFullPath(relativePath));
		}

		public static string FindExtension(string dir, string fileName)
		{
			string pathNoExt = Path.Combine(dir, fileName);
			string ext = ".png";
			string relativePath = pathNoExt + ext;
			if (!File.Exists(GetFullPath(relativePath)))
			{
				ext = ".jpg";
				relativePath = pathNoExt + ext;
				if (!File.Exists(GetFullPath(relativePath)))
				{
					ext = ".jpeg";
					relativePath = pathNoExt + ext;
					if (!File.Exists(GetFullPath(relativePath)))
					{
						return "";
					}
				}
			}
			return ext;
		}
	}
}
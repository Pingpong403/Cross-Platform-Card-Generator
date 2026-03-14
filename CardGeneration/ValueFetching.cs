using SkiaSharp;

namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class ValueFetching
	{
		public static string GetConfigValue(string configFile, string key)
		{
			string path = Structuring.GetFullPath(Path.Combine("config", configFile + "-config.txt"));
			if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					// Split line into key, value pair
					string[] pair = line.Split(":");

					// If key matches given key, return the value
					if (string.Equals(pair[0], key))
					{
						return pair[1];
					}

					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			
			return "";
		}

		public static string GetSettingsValue(string settingsFile, string setting)
		{
			string path = Structuring.GetFullPath(Path.Combine("Card Data", "-Settings", settingsFile + "Settings.txt"));
			if (!File.Exists(path))
            {
                return "";
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					if (line != "")
					{
					if (line[0] != '#')
						{
							// Split line into key, value pair
							string[] pair = line.Split(":");

							// If right side is set, return true
							if (pair[0] == setting && pair[1] != "")
							{
								return pair[1];
							}
						}
					}

					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			
			return "";
		}

		public static Dictionary<string, string> GetColorMapping()
		{
			Dictionary<string, string> baseKeywordColors = [];
			Dictionary<string, string> keywordsAndColors = [];

			// Populate dictionary to hold each singular keyword and its corresponding color
			foreach (string line in GetTextFilesLines("Colors"))
			{
				string[] lineSplit = line.Split("|");
				// If color exists in -TextFiles\Colors.txt, use that color.
				// If not, use color found in config\color-config.txt
				if (lineSplit.Length == 1)
				{
					// Search through color-config.txt for correct color
					string searchParam = lineSplit[0].ToLower() + "Color";
					baseKeywordColors[lineSplit[0]] = ValueFetching.GetConfigValue("color", searchParam);
				}
				else
				{
					// First part is the base keyword, second part is its color
					baseKeywordColors[lineSplit[0]] = lineSplit[1];
				}
			}

			// Link each keyword variant to its singular form and therefore its correct color
			foreach (string line in GetTextFilesLines("Keywords"))
			{
				string[] lineSplit = line.Split("|");
				foreach (string variant in lineSplit)
				{
					if (variant != "")
					{
						if (!baseKeywordColors.TryGetValue(lineSplit[0], out string? value))
						{
							keywordsAndColors[variant] = ValueFetching.GetConfigValue("color", "fontColor");
						}
						else
						{
							keywordsAndColors[variant] = value == "" ? ValueFetching.GetConfigValue("color", "fontColor") : value;
						}
					}
				}
			}

			return keywordsAndColors;
		}

		public static List<string> GetTextFilesLines(string file)
		{
			List<string> lines = [];
			string path = Structuring.GetFullPath(Path.Combine("Card Data", "-TextFiles", file + ".txt"));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Text file not found: {path}");
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				// Continue to read until you reach end of file
				while (line != null)
				{
					// Skip empty lines
					if (line != "")
					{
						// '#' denotes a comment line
						if (line[0] != '#')
						{
							// Add the line to the list
							lines.Add(line);
						}
					}
					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}

			return lines;
		}

		public static SKPoint GetElementPos(string element)
		{
			int elementCenterX = int.Parse(GetConfigValue("layout", element + "CenterX"));
			int elementCenterY = int.Parse(GetConfigValue("layout", element + "CenterY"));
			return new SKPoint(elementCenterX, elementCenterY);
		}
	}
}
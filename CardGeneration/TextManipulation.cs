namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class TextManipulation
	{
		public static string CleanTitle(string title)
		{
			string cleanTitle = "";

			bool alreadyAddedSpace = false;
			foreach (char letter in title)
			{
				// For spaces, only add one between words
				if (letter == ' ')
				{
					if (!alreadyAddedSpace)
					{
						cleanTitle += ' ';
						alreadyAddedSpace = true;
					}
				}

				// For newline characters, remove them and add a space
				else if (letter == '%' || letter == '\n')
				{
					if (!alreadyAddedSpace)
					{
						cleanTitle += ' ';
						alreadyAddedSpace = true;
					}
				}

				// For everything else, only add the letter
				else
				{
					cleanTitle += letter;
					alreadyAddedSpace = false;
				}
			}
			while (IsPunctuation(char.ToString(cleanTitle[^1])))
			{
				cleanTitle = cleanTitle[0..^1];
			}
			return cleanTitle;
		}

		public static string Capitalize(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

		public static bool IsPunctuation(string text)
		{
			if (".?!,;:/-".Contains(text))
			{
				return true;
			}
			return false;
		}

		public static string GetAssetName(string assetCode)
		{
			if (assetCode == "") return "";
			string assetCharacter = ValueFetching.GetConfigValue("text", "assetCharacter");
			if (assetCode[^1].ToString() != assetCharacter) return "";
			char[] letters = new char[assetCode.Length - 1];
			for (int i = 0; i < assetCode.Length - 1; i++)
			{
				letters[i] = assetCode[i];
			}

			return new string(letters);
		}

		public static string GainPowerAmount(string gainsActionCode)
		{
			char[] letters = gainsActionCode.ToCharArray();
			if (letters.Length >= 10)
			{
				// GainXPower
				string gain = new(letters[0..4]);
				string power = new(letters[(letters.Length - 5)..letters.Length]);
				if (Equals(gain, "Gain") && Equals(power, "Power"))
				{
					return new string(letters[4..(letters.Length - 5)]);
				}
			}
			return "";
		}
	}
}
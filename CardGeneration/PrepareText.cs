using System.Drawing;
using System.Transactions;
using SkiaSharp;

namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class PrepareText
	{
		public static void DrawTitle(string text, string fontName, int fontSize, SKColor color, int maxWidth, int maxHeight)
		{
			// Setup variables
			float granularity = float.Parse(ValueFetching.GetConfigValue("text", "titleFontDecreaseGranularity"));
			float lineSpacingFactor = float.Parse(ValueFetching.GetConfigValue("text", "titleLineSpacingFactor"));
			SKFont font = FontLoader.GetFont(fontName, fontSize, SKFontStyle.Normal);

			// Remove duplicate designation
			if (text[^1] == ')' && text[^3] == '(')
			{
				if (text[^4] == ' ') text = text[0..^4];
				else text = text[0..^3];
			}

			// For titles, capitalize the text
			text = text.ToUpper();

			using var b = new SKBitmap(maxWidth, maxHeight);
			using var c = new SKCanvas(b);
			c.Clear(SKColors.Transparent);
			List<CardWord> words = GetCardWords(text, color, fontName, fontSize, null);

			// Find proper font size given line number
			float lineHeight;
			SKSize textSize;
			do
			{
				words = GetCardWords(text, color, fontName, fontSize, null);
				lineHeight = font.Size * lineSpacingFactor;
				textSize = MeasureWordByWord(words, 10000, lineHeight, lineSpacingFactor);
				if (textSize.Height > maxHeight) font = new SKFont(font.Typeface, font.Size - granularity);
			} while (textSize.Height > maxHeight);

			// Find the proper squish ratio for given title
			if (textSize.Width > maxWidth)
			{
				float horizontalSquish = maxWidth / textSize.Width;
				c.Scale(horizontalSquish, 1.0F);
				maxWidth = (int)textSize.Width;
			}

			// Set up variables
			float startY = (maxHeight - textSize.Height) / 2;

			DrawWordByWord(words, c, maxWidth, lineHeight, maxWidth / 2, startY, lineSpacingFactor);

			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			using var outpath = File.OpenWrite(Path.Combine(outDir, "Title.png"));
			b.Encode(outpath, SKEncodedImageFormat.Png, 100);
		}

		public static void DrawAbility(string ability, string activateAbility, string activateCost, string gainsAction, string fontName, int fontSize, SKColor color, int maxWidth, int maxHeight, Dictionary<string, string> keywordsAndColors)
		{
			// Set up variables we'll potentially need
			float granularity = float.Parse(ValueFetching.GetConfigValue("text", "fontDecreaseGranularity"));
			float paddingLines = float.Parse(ValueFetching.GetConfigValue("text", "abilityPaddingLines"));
			float minFontSize = float.Parse(ValueFetching.GetConfigValue("text", "abilityMinFontSize"));
			float actionSymbolLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			float lineSpacing = float.Parse(ValueFetching.GetConfigValue("text", "lineSpacingFactor"));
			int abilityBottomPadding = int.Parse(ValueFetching.GetConfigValue("card", "abilityBottomPadding"));
			int sideAAMaxW = int.Parse(ValueFetching.GetConfigValue("card", "sideActivateAbilityMaxWidth"));
			int sideAACenterX = int.Parse(ValueFetching.GetConfigValue("card", "sideActivateAbilityCenterX"));
			bool useAltAssets = ValueFetching.GetSettingsValue("Card", "useAlternateAssets") == "true";
			SKFont font = FontLoader.GetFont(fontName, fontSize, SKFontStyle.Normal);

			using var b = new SKBitmap(maxWidth, maxHeight);
			using var c = new SKCanvas(b);
			c.Clear(SKColors.Transparent);

			// Naively find the maximum allowable font size by measuring everything
			float lineHeight;
			float abilityHeight;
			float activateAbilityHeight;
			bool activateAbilityTextTaller = true;
			float gainsActionHeight;
			float paddingHeight;
			float textHeight;
			do
			{
				// Combine every given ability into one metric
				lineHeight = font.Size * lineSpacing;
				abilityHeight = ability == "" ? 0 : MeasureWordByWord(GetCardWords(ability, color, fontName, fontSize, keywordsAndColors), maxWidth, lineHeight, lineSpacing).Height;
				activateAbilityHeight = 0;
				if (activateAbility != "" || activateCost != "")
				{
					if (ability == "" || activateCost != "") // If there is no ability or there is an activate cost, measure normally
					{
						activateAbilityHeight += actionSymbolLines * lineHeight + MeasureWordByWord(GetCardWords(activateAbility, color, fontName, fontSize, keywordsAndColors), maxWidth, lineHeight, lineSpacing).Height;
					}
					else
					{
						float aaTextHeight = MeasureWordByWord(GetCardWords(activateAbility, color, fontName, fontSize, keywordsAndColors), sideAAMaxW, lineHeight, lineSpacing).Height;
						float aaSymbolHeight = actionSymbolLines * lineHeight;
						activateAbilityTextTaller = aaTextHeight > aaSymbolHeight;
						activateAbilityHeight += Math.Max(aaSymbolHeight, aaTextHeight);
					}
				}
				gainsActionHeight = gainsAction == "" ? 0 : MeasureWordByWord(GetCardWords(gainsAction, color, fontName, fontSize, keywordsAndColors), maxWidth, lineHeight, lineSpacing).Height;
				int numPadding = (abilityHeight > 0 ? 1 : 0) + (activateAbilityHeight > 0 ? 1 : 0) + (gainsActionHeight > 0 ? 1 : 0) - 1;
				if (numPadding < 0) numPadding = 0;
				paddingHeight = numPadding * lineHeight * paddingLines;
				textHeight = abilityHeight + activateAbilityHeight + gainsActionHeight + paddingHeight;

				if (textHeight > maxHeight - (activateAbility != "" && ability == "" ? textHeight * 0.1 : 0)) font = new SKFont(font.Typeface, font.Size - granularity);
			} while (textHeight > maxHeight);

			// Check if font size went below minimum and notify the user
			if (font.Size < minFontSize)
			{
				Console.WriteLine($"The following card's Ability went below the minimum {minFontSize}px:");
			}

			List<CardWord> colon = GetCardWords(":", color, fontName, fontSize, keywordsAndColors);

			// Drawing variables
			float currentY = (maxHeight - textHeight) / 2;
			List<CardWord> words;

			// Draw the ability first
			if (ability != "")
			{
				words = GetCardWords(ability, color, fontName, fontSize, keywordsAndColors);
				currentY = DrawWordByWord(words, c, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
				currentY += lineHeight * paddingLines;
			}

			// Then draw the activate symbol, cost, and ability
			if (activateAbility != "" || activateCost != "")
			{
				// Symbol
				string assetName = "Activate";
				if (useAltAssets) assetName += ValueFetching.GetConfigValue("asset", "alternateDesignation");
				string activateSymbolPath = Structuring.GetFullPath(Path.Combine("assets", assetName + Structuring.FindExtension("assets", assetName)));
				SKImage activateSymbol = SKImage.FromEncodedData(activateSymbolPath);
				float resizing = actionSymbolLines * lineHeight / activateSymbol.Height;
				float symbolCenterX = maxWidth / 2;
				if (ability == "" || activateCost != "") // If there is no ability or there is an activate cost, draw normally
				{
					int colonCenterX = int.Parse(ValueFetching.GetConfigValue("card", "colonCenterX"));
					int colonPadding = int.Parse(ValueFetching.GetConfigValue("card", "colonPadding"));
					bool drawColon = activateCost != "" && activateCost[0..4] == "Pay " && (activateCost[^6..^0] == " Power" || activateCost[^7..^0] == " Power.");
					float symbolW = activateSymbol.Width * resizing;
					if (activateCost != "")
					{
						symbolCenterX = colonCenterX - colonPadding - symbolW / 2;
					}
					if (ability == "" && activateAbility != "")
					{
						currentY = lineHeight * 0.1F;
					}
					DrawSymbol(activateSymbol, c, symbolCenterX, currentY + actionSymbolLines * lineHeight / 2, resizing);
					
					// Cost, if any
					if (activateCost != "")
					{
						float costLeftX = colonCenterX + colonPadding;
						float activateCostWidth = maxWidth / 2;
						float activateCostHeight = MeasureWordByWord(GetCardWords(activateCost, color, fontName, fontSize, keywordsAndColors), activateCostWidth, lineHeight, lineSpacing).Height;
						float activateCostY = currentY + (actionSymbolLines * lineHeight - activateCostHeight) / 2; // maximum of 3 lines for clarity
						if (drawColon)
						{
							float costCenterX = costLeftX + font.MeasureText(activateCost) / 2;
							DrawWordByWord(colon, c, maxWidth, lineHeight, colonCenterX, activateCostY, lineSpacing);
							words = GetCardWords(activateCost, color, fontName, fontSize, keywordsAndColors);
							DrawWordByWord(words, c, activateCostWidth, lineHeight, costCenterX, activateCostY, lineSpacing);
						}
						else
						{
							float costCenterX = costLeftX + font.MeasureText(activateCost) / 2;
							words = GetCardWords(activateCost, color, fontName, fontSize, keywordsAndColors);
							DrawWordByWord(words, c, activateCostWidth, lineHeight, maxWidth / 2 + costCenterX, activateCostY, lineSpacing);
						}
					}
					currentY += actionSymbolLines * lineHeight;

					// Ability, if any
					if (activateAbility != "")
					{
						words = GetCardWords(activateAbility, color, fontName, fontSize, keywordsAndColors);
						currentY = DrawWordByWord(words, c, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
					}
				}
				else // Otherwise, the activate ability is to the right of the symbol
				{
					// Symbol
					symbolCenterX = sideAACenterX - sideAAMaxW / 2 - 100 - activateSymbol.Width * resizing / 2;
					DrawSymbol(activateSymbol, c, symbolCenterX, currentY + activateAbilityHeight / 2, resizing);
					
					// Activate ability
					words = GetCardWords(activateAbility, color, fontName, fontSize, keywordsAndColors);
					float drawY = currentY;
					if (!activateAbilityTextTaller)
					{
						drawY += (activateAbilityHeight - MeasureWordByWord(words, sideAAMaxW, lineHeight, lineSpacing).Height) / 2;
					}
					DrawWordByWord(words, c, sideAAMaxW, lineHeight, maxWidth - sideAAMaxW / 2 - 30, drawY, lineSpacing);
					currentY += activateAbilityHeight;
				}
				currentY += lineHeight * paddingLines;
			}

			// Finally, draw the gained action
			if (gainsAction != "")
			{
				words = GetCardWords(gainsAction, color, fontName, fontSize, keywordsAndColors);
				DrawWordByWord(words, c, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
			}

			// Ensure output directory exists and save per-element PNG
			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			using var outpath = File.OpenWrite(Path.Combine(outDir, "Ability.png"));
			b.Encode(outpath, SKEncodedImageFormat.Png, 100);
		}

		public static void DrawType(string text, SKFont font, SKColor color, int maxWidth, int maxHeight, Dictionary<string, string> keywordsAndColors)
		{
			var info = new SKImageInfo(maxWidth, maxHeight);
			var s = SKSurface.Create(info);

			string title = "Test Card";
			SKCanvas c = s.Canvas;

			c.Clear(SKColors.Transparent);

			// c.DrawText(title);

			c.Dispose();
			s.Dispose();
		}

		public static void DrawCornerElement(string text, SKFont font, SKColor color, string element, int maxWidth, int maxHeight)
		{
			var info = new SKImageInfo(maxWidth, maxHeight);
			var s = SKSurface.Create(info);

			string title = "Test Card";
			SKCanvas c = s.Canvas;

			c.Clear(SKColors.Transparent);

			// c.DrawText(title);

			c.Dispose();
			s.Dispose();
		}

		public static SKSize MeasureWordByWord(List<CardWord> words, float maxWidth, float lineHeight, float lineSpacing)
		{
			float lineBreakLines = float.Parse(ValueFetching.GetConfigValue("text", "lineBreakLines"));
			float actionSymbolLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			float dividingLineLines = float.Parse(ValueFetching.GetConfigValue("asset", "dividingLineLines"));

			float longestLine = 0;
			float textHeight = 0;
			float lineWidth = 0;
			int consecutiveLineBreakCount = 0;
			bool space = false;
			float spaceWidth = 0;
			foreach (CardWord word in words)
			{
				// Keywords: add the amount of vertical space they take up
				if (Structuring.AssetExists(word.GetText()))
				{
					consecutiveLineBreakCount = 0;
					if (lineWidth > 0) // Check if some words have already been added to line
					{
						textHeight += lineHeight;
						if (lineWidth > longestLine) longestLine = lineWidth;
						lineWidth = 0;
						space = false;
					}
					if (word.GetText() == "DividingLine_") textHeight += dividingLineLines * lineHeight;
					else textHeight += actionSymbolLines * lineHeight;
				}
				// Spaces: set flag
				else if (word.GetText() == " ")
				{
					consecutiveLineBreakCount = 0;
					space = true;
					spaceWidth = word.GetSizeF().Width;
				}
				// Newlines: add a line
				else if (word.GetText() == "\n")
				{
					consecutiveLineBreakCount++;
					switch (consecutiveLineBreakCount % 3)
					{
						case 1:
							textHeight += lineHeight;
							break;
						case 2:
							textHeight += lineHeight * lineBreakLines;
							break;
						case 0:
							textHeight += lineHeight * (1.0F - lineBreakLines);
							break;
						default:
							break;
					}
					if (lineWidth > longestLine) longestLine = lineWidth;
					lineWidth = 0.001F; // Completely ignore the text that was already built up
				}
				// Generic case: add word's width (+ space), check if over
				else
				{
					consecutiveLineBreakCount = 0;
					float wordWidth = word.GetSizeF().Width + (space ? spaceWidth : 0);
					lineWidth += wordWidth;
					if (lineWidth > maxWidth)
					{
						textHeight += lineHeight;
						if (lineWidth > longestLine) longestLine = lineWidth - wordWidth;
						lineWidth = word.GetSizeF().Width;
					}
					space = false;
				}
			}
			if (lineWidth > 0)
			{
				textHeight += lineHeight;
				if (lineWidth > longestLine) longestLine = lineWidth;
			}

			// Add the end of the last line that was culled
			textHeight += (1 - lineSpacing) * lineHeight * lineSpacing;
			return new SKSize(longestLine, textHeight);
		}

		private static float DrawWordByWord(List<CardWord> words, SKCanvas c, float maxWidth, float lineHeight, float centerX, float startY, float lineSpacing)
		{
			// Set up variables we'll potentially need
			float lineBreakLines = float.Parse(ValueFetching.GetConfigValue("text", "lineBreakLines"));
			float dlLines = float.Parse(ValueFetching.GetConfigValue("asset", "dividingLineLines"));
			float asLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			int horizontalPadding = int.Parse(ValueFetching.GetConfigValue("text", "wordHorizontalPadding"));
			bool useAltAssets = ValueFetching.GetSettingsValue("Card", "useAlternateAssets") == "true";
			Color color = ColorTranslator.FromHtml("#" + ValueFetching.GetConfigValue("color", "fontColor"));

			// Draw text word by word
			float currentY = startY;
			int iCheck = 0;
			int iDraw = 0;
			float lineLength;
			bool skipLine;
			bool countLineBreaks;
			bool drawAsset = false;
			bool endOfText = false;
			while (!endOfText)
			{
				// First, find the length of this line
				lineLength = 0;
				skipLine = false;
				countLineBreaks = false;
				try
				{
					bool endOfLine = false;
					float currentWordWidth;
					bool space = false;
					float spaceWidth = 0;
					while (!endOfLine)
					{
						// Measure each word
						currentWordWidth = words[iCheck].GetSizeF().Width;

						if (words[iCheck].GetText() == " ")
						{
							if (lineLength == 0)
							{
								space = true;
								spaceWidth = 0;
								iDraw++;
							}
							else
							{
								space = true;
								spaceWidth = words[iCheck].GetSizeF().Width;
							}
							iCheck++;
						}
						else if (Structuring.AssetExists(words[iCheck].GetText()))
						{
							endOfLine = true;
							if (lineLength == 0) skipLine = true;
							drawAsset = true;
						}
						else if (words[iCheck].GetText() == "\n")
						{
							endOfLine = true;
							countLineBreaks = true;
							iCheck++;
						}
						else if (lineLength + currentWordWidth + (space ? spaceWidth : 0) > maxWidth)
						{
							endOfLine = true;
						}
						else
						{
							lineLength += currentWordWidth + (space ? spaceWidth : 0);
							space = false;
							iCheck++;
						}
					}
				}
				catch (Exception ex)            
				{                
					if (ex is IndexOutOfRangeException || ex is ArgumentOutOfRangeException)
					{
						endOfText = true;
					}
					else
						throw;
				}
				// Draw each word in the line
				float currentX = centerX - lineLength / 2;
				for (int i = iDraw; i < iCheck; i++)
				{
					CardWord word = words[i];
					if (word.GetText() != "\n")
					{
						float wordWidth = (word.GetText() != " " || currentX > centerX - lineLength / 2) ? word.GetSizeF().Width : 0;
						using var paint = new SKPaint
						{
							Color = word.GetTextColor(),
							Style = SKPaintStyle.Fill
						};
						c.DrawText(word.GetText(), new SKPoint(currentX, currentY + lineHeight), SKTextAlign.Left, word.GetTextFont(), paint);
						currentX += wordWidth;
						iDraw++;
					}
				}
				// Move the register down
				if (!skipLine)
				{
					if (countLineBreaks)
					{
						int consecutiveLineBreakCount = 0;
						while (words[iDraw].GetText() == "\n")
						{
							consecutiveLineBreakCount++;
							switch (consecutiveLineBreakCount % 3)
							{
								case 1:
									currentY += lineHeight;
									break;
								case 2:
									currentY += lineHeight * lineBreakLines;
									break;
								case 0:
									currentY += lineHeight * (1.0F - lineBreakLines);
									break;
								default:
									break;
							}
							iDraw++;
						}
						iCheck = iDraw;
					}
					else currentY += lineHeight;
				}
				// Draw the asset that is up to draw
				if (drawAsset)
				{
					string assetName = TextManipulation.GetAssetName(words[iDraw].GetText());
					string gainPowerAmt = TextManipulation.GainPowerAmount(assetName);
					if (gainPowerAmt != "")
					{
						assetName = "GainPower";
					}
					if (useAltAssets) assetName += ValueFetching.GetConfigValue("asset", "alternateDesignation");
					string gainsSymbolPath = Structuring.GetFullPath(Path.Combine("assets", assetName + Structuring.FindExtension("assets", assetName)));
					SKImage asset = SKImage.FromEncodedData(gainsSymbolPath);
					float resizing = string.Equals(assetName, "DividingLine") || string.Equals(assetName, "DividingLine" + ValueFetching.GetConfigValue("asset", "alternateDesignation")) ? 1.0F : asLines * lineHeight / asset.Height;
					float yOffset = string.Equals(assetName, "DividingLine") || string.Equals(assetName, "DividingLine" + ValueFetching.GetConfigValue("asset", "alternateDesignation")) ? dlLines * lineHeight / 2 : asLines * lineHeight / 2;
					DrawSymbol(asset, c, maxWidth / 2, currentY + yOffset, resizing);

					// If this was a Gain Power action, draw the amount to be gained
					if (gainPowerAmt != "")
					{
						SKFont gainPowerFont = FontLoader.GetFont(
							ValueFetching.GetConfigValue("text", "elementFont"),
							(int)(float.Parse(ValueFetching.GetConfigValue("text", "costFontSize")) * resizing),
							SKFontStyle.Bold
						);
						SKPoint gainPowerPos = new(
							(int)maxWidth,
							(int)(currentY + asLines * lineHeight / 2)
						);
						c.Clear(SKColors.Transparent);
						using var paint = new SKPaint
						{
							Color = ColorConverter.FromHtml(ValueFetching.GetConfigValue("color", "textColor")),
							Style = SKPaintStyle.Fill
						};
						int wordHeight = (int)gainPowerFont.Size;
						c.DrawText(gainPowerAmt, new SKPoint(0, (int)((lineHeight - wordHeight) * lineSpacing / 2)), SKTextAlign.Center, gainPowerFont, paint);
					}
					currentY += lineHeight * (string.Equals(assetName, "DividingLine") ? dlLines : asLines);
					iCheck++;
					iDraw++;
					drawAsset = false;
				}
			}
			return currentY;
		}

		private static void DrawSymbol(SKImage symbol, SKCanvas c, float centerX, float centerY, float resizing = 1.0F)
		{
			int newWidth = (int)(symbol.Width * resizing);
			int newHeight = (int)(symbol.Height * resizing);
			var resized = new SKBitmap(newWidth, newHeight);
			using (var newC = new SKCanvas(resized))
			{
				newC.Clear(SKColors.Transparent);
				newC.DrawImage(symbol, new SKRect(0, 0, newWidth, newHeight));
			}
			float x = centerX - resizing * symbol.Width / 2;
			float y = centerY - resizing * symbol.Height / 2;
			c.DrawBitmap(resized, new SKPoint(x, y));
		}

		public static List<CardWord> GetCardWords(string text, SKColor defaultColor, string defaultFontName, int defaultFontSize, Dictionary<string, string>? keywordData, bool isType = false)
		{
			bool typeIsCaps = ValueFetching.GetConfigValue("text", "typeIsCaps") == "true";
			bool typeInAbilityIsBold = ValueFetching.GetConfigValue("text", "typeInAbilityIsBold") == "true";
			
			char italicSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "italicCharacter"));
			char boldSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "boldCharacter"));
			char escapeSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "escapeCharacter"));
			char newlineSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "newlineCharacter"));

			SKFont defaultFont = FontLoader.GetFont(defaultFontName, defaultFontSize, SKFontStyle.Normal);
			SKFont italicFont = FontLoader.GetFont(defaultFontName, defaultFontSize, SKFontStyle.Italic);
			SKFont boldFont = FontLoader.GetFont(defaultFontName, defaultFontSize, SKFontStyle.Bold);
			SKFont boldItalicFont = FontLoader.GetFont(defaultFontName, defaultFontSize, SKFontStyle.BoldItalic);

			List<CardWord> cardWords = [];

			bool italicsOpen = false;
			bool boldOpen = false;
			bool escapeNext = false;
			bool ignoreFormatting = false;
			string builtWord = "";
			foreach (char letter in text)
			{
				if (letter == ' ')
				{
					// End of word
					if (builtWord != "")
					{
						bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
						bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
									   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
						CardWord word = new(
							builtWord,
							isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
							boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
						);
						word.SetType(isType);
						if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
						cardWords.Add(word);
					}
					cardWords.Add(new CardWord(" ", defaultColor, defaultFont));
					builtWord = "";
					ignoreFormatting = false;
				}
				else if (escapeNext)
				{
					// If the next was a symbol, then we escape it
					if (letter == italicSymbol ||
						letter == boldSymbol   ||
						letter == escapeSymbol ||
						letter == newlineSymbol
						)
					{
						cardWords.Add(new CardWord(Convert.ToString(letter), defaultColor, defaultFont));
					}

					// Otherwise, this means the next word should not be formatted
					else
					{
						if (builtWord == "") ignoreFormatting = true;
						builtWord += letter;
					}
					escapeNext = false;
				}
				else
				{
					if (letter == italicSymbol)
					{
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
										   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
						italicsOpen = !italicsOpen;
					}
					else if (letter == boldSymbol)
					{
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
										   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
						boldOpen = !boldOpen;
					}
					else if (letter == escapeSymbol)
					{
						escapeNext = true;
					}
					else if (letter == newlineSymbol || letter == '\n')
					{
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
									   	   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
						cardWords.Add(new CardWord("\n", defaultColor, defaultFont));
						ignoreFormatting = false;
					}
					else
					{
						if (TextManipulation.IsPunctuation(Convert.ToString(letter)) && builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
										   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							cardWords.Add(new CardWord(Convert.ToString(letter), defaultColor, defaultFont));
							builtWord = "";
							ignoreFormatting = false;
						}
						// Most generic case - add a letter to builtWord
						else
						{
							builtWord += letter;
						}
					}
				}
			}
			if (builtWord != "")
			{
				bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
				bool boldWord = (isKeyword && (isType || typeInAbilityIsBold)) && !boldOpen ||
							   !(isKeyword && (isType || typeInAbilityIsBold)) && boldOpen;
				CardWord word = new(
					builtWord,
					isKeyword && !ignoreFormatting ? ColorConverter.FromHtml(keywordData[builtWord]) : defaultColor,
					boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
				);
				word.SetType(isType);
				if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
				cardWords.Add(word);
			}

			return cardWords;
		}

		public class CardWord
		{
			private string text;
			private SKColor textColor;
			private SKFont textFont;
			private bool isType = false;

			public CardWord()
			{
				text = "";
				textColor = SKColors.Black;
				textFont = FontLoader.GetFont(ValueFetching.GetConfigValue("text", "altFont"), 1, SKFontStyle.Normal);
			}

			public CardWord(string text)
			{
				this.text = text;
				textColor = SKColors.Black;
				textFont = FontLoader.GetFont(ValueFetching.GetConfigValue("text", "altFont"), 1, SKFontStyle.Normal);
			}

			public CardWord(string text, SKColor textColor, SKFont textFont)
			{
				this.text = text;
				this.textColor = textColor;
				this.textFont = textFont;
			}

			public CardWord(CardWord other)
			{
				text = other.GetText();
				textColor = other.GetTextColor();
				textFont = other.GetTextFont();
			}

			public string GetText() { return text; }
			public SKColor GetTextColor() { return textColor; }
			public SKFont GetTextFont() { return textFont; }
			public bool IsType() { return isType; }

			public void SetText(string text) { this.text = text; }
			public void SetTextBrush(SKColor textColor) { this.textColor = textColor; }
			public void SetTextFont(SKFont textFont) { this.textFont = textFont; }
			public void SetType(bool isType) { this.isType = isType; }

			public SKSize GetSizeF()
			{
				if (text != " ")
				{
					using var path = textFont.GetTextPath(text);
					SKRect bounds = path.Bounds;
					return new SKSize(bounds.Width * (!TextManipulation.IsPunctuation(text) ? 1.05f : 1f), bounds.Height);
				}
				else
				{
					var paint = new SKPaint{ Color = textColor, Style = SKPaintStyle.Fill };
					return new SKSize(textFont.MeasureText(text), textFont.Size);
				}
			}
		}
	}
}
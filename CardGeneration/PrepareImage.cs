using SkiaSharp;

namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class PrepareImage
	{
		public static void SizeCardImage(string imageName)
		{
			// Get image into an object
			var dir = Path.Combine("Card Data", "-Images");
			var relativePath = Path.Combine(dir, imageName) + Structuring.FindExtension(dir, imageName);
            var fullPath = Structuring.GetFullPath(relativePath);
			// If no image is found, continue on with black background
			if (!File.Exists(fullPath))
			{
				relativePath = Path.Combine("assets", "black_bg.png");
				fullPath = Structuring.GetFullPath(relativePath);
			}
			using var input = File.OpenRead(fullPath);
			using var original = SKBitmap.Decode(input);

			float targetHeight = float.Parse(ValueFetching.GetConfigValue("card", "imageAreaHeight"));
			float ratio = targetHeight / original.Height;
			int newWidth = Math.Max(1, (int)Math.Round(ratio * original.Width));
			int newHeight = Math.Max(1, (int)Math.Round(ratio * original.Height));

			using var resized = new SKBitmap(newWidth, newHeight);

			// Draw image with new width and height
			using (var canvas = new SKCanvas(resized))
			{
				canvas.Clear(SKColors.Black);
				canvas.DrawBitmap(original, new SKRect(0, 0, newWidth, newHeight));
			}

			// Ensure output directory exists and save resized PNG
			var relativeOutDir = Path.Combine("temp", "ImageIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			using var outpath = File.OpenWrite(Path.Combine(outDir, $"{imageName}.png"));
			resized.Encode(outpath, SKEncodedImageFormat.Png, 100);
		}

		public static void CombineImages(string cardTitle, string deck)
		{
			string capitalizedDeck = TextManipulation.Capitalize(deck.ToLower());
			string cleanTitle = TextManipulation.CleanTitle(cardTitle);

			var imageIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "ImageIntermediary"));
			var textIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "TextIntermediary"));
			var layoutPath = Structuring.GetFullPath(Path.Combine("Card Data", "-Layout"));
			var assetsPath = Structuring.GetFullPath("assets");

			// All possible elements: image, Title, Ability, Type,
			// Cost, Strength, TopRight, BottomRight
			var imagePath = Path.Combine(imageIntermediaryPath, cleanTitle + ".png");
			var altImagePath = Path.Combine(assetsPath, "black_bg.png");
			var titlePath = Path.Combine(textIntermediaryPath, "Title.png");
			var abilityPath = Path.Combine(textIntermediaryPath, "Ability.png");
			var typePath = Path.Combine(textIntermediaryPath, "Type.png");
			var costPath = Path.Combine(textIntermediaryPath, "Cost.png");
			var strengthPath = Path.Combine(textIntermediaryPath, "Strength.png");
			var topRightPath = Path.Combine(textIntermediaryPath, "TopRight.png");
			var bottomRightPath = Path.Combine(textIntermediaryPath, "BottomRight.png");

			int cardWidth = int.Parse(ValueFetching.GetConfigValue("card", "w"));
			int cardHeight = int.Parse(ValueFetching.GetConfigValue("card", "h"));
			string resolutionValue = ValueFetching.GetSettingsValue("Data", "outputResolution") != "" ? ValueFetching.GetSettingsValue("Data", "outputResolution")[0..^1] : "100";
			float resolution = float.Parse(resolutionValue) / 100F;
			using var b = new SKBitmap(cardWidth, cardHeight);
			using var c = new SKCanvas(b);

			// Only draw any elements if all of the necessary elements are present
			// Necessary elements: Title, Ability, Type
			if (!(File.Exists(titlePath) && File.Exists(abilityPath) && File.Exists(typePath)))
			{
				Console.WriteLine($"Missing Title, Ability, or Type field(s) for {cleanTitle}!");
				return;
			}

			// Card image
			bool imageExists = File.Exists(imagePath);
			using (SKImage cardImg = SKImage.FromEncodedData(imageExists ? imagePath : altImagePath))
			{
				// get image width
				int imgWidth = cardImg.Width;
				// set xOffset to appropriate value
				int xOffset = (cardWidth - imgWidth) / 2;
				c.DrawImage(cardImg, xOffset, 0);
			}
			
			// Deck background
			if (!Structuring.ElementExists(capitalizedDeck, "Deck"))
			{
				Console.WriteLine($"Missing {capitalizedDeck}Deck.png for {cleanTitle}.");
				return;
			}
			using (SKImage backgroundImg = SKImage.FromEncodedData(Path.Combine(layoutPath, capitalizedDeck + "Deck.png")))
			{
				c.DrawImage(backgroundImg, new SKRect(0, 0, cardWidth, cardHeight));
			}

			// Title
			using (SKImage titleImg = SKImage.FromEncodedData(titlePath))
			{
				SKPoint titleCenter = ValueFetching.GetElementPos("title");
				c.DrawImage(titleImg, titleCenter.X - titleImg.Width / 2, titleCenter.Y - titleImg.Height / 2);
			}
			
			// Ability
			using (SKImage abilityImg = SKImage.FromEncodedData(abilityPath))
			{
				int abilityCenterX = int.Parse(ValueFetching.GetConfigValue("layout", "abilityCenterX"));
				int abilityTopY = int.Parse(ValueFetching.GetConfigValue("layout", "abilityTopY"));
				int abilityBottomPadding = int.Parse(ValueFetching.GetConfigValue("card", "abilityBottomPadding"));
				c.DrawImage(abilityImg, abilityCenterX - abilityImg.Width / 2, abilityTopY);
			}

			// Type
			using (SKImage typeImg = SKImage.FromEncodedData(typePath))
			{
				SKPoint typeCenter = ValueFetching.GetElementPos("type");
				c.DrawImage(typeImg, typeCenter.X - typeImg.Width / 2, typeCenter.Y - typeImg.Height / 2);
			}
			
			// Cost
			if (File.Exists(costPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "Cost"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}Cost.png for {cleanTitle}.");
					return;
				}
				c.DrawImage(SKImage.FromEncodedData(Path.Combine(layoutPath, capitalizedDeck + "Cost.png")), new SKRect(0, 0, cardWidth, cardHeight));
				using (SKImage costImg = SKImage.FromEncodedData(costPath))
				{
					SKPoint costCenter = ValueFetching.GetElementPos("cost");
					c.DrawImage(costImg, costCenter.X - costImg.Width / 2, costCenter.Y - costImg.Height / 2);
				}
			}
			
			// Strength
			if (File.Exists(strengthPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "Strength"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}Strength.png for {cleanTitle}.");
					return;
				}
				c.DrawImage(SKImage.FromEncodedData(Path.Combine(layoutPath, capitalizedDeck + "Strength.png")), new SKRect(0, 0, cardWidth, cardHeight));
				using (SKImage strengthImg = SKImage.FromEncodedData(strengthPath))
				{
					SKPoint strengthCenter = ValueFetching.GetElementPos("strength");
					c.DrawImage(strengthImg, strengthCenter.X - strengthImg.Width / 2, strengthCenter.Y - strengthImg.Height / 2);
				}
			}

			// Top Right Element
			if (File.Exists(topRightPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "TopRight"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}TopRight.png for {cleanTitle}.");
					return;
				}
				c.DrawImage(SKImage.FromEncodedData(Path.Combine(layoutPath, capitalizedDeck + "TopRight.png")), new SKRect(0, 0, cardWidth, cardHeight));
				using (SKImage topRightImg = SKImage.FromEncodedData(topRightPath))
				{
					SKPoint topRightCenter = ValueFetching.GetElementPos("topRight");
					c.DrawImage(topRightImg, topRightCenter.X - topRightImg.Width / 2, topRightCenter.Y - topRightImg.Height / 2);
				}
			}

			// Bottom Right Element
			if (File.Exists(bottomRightPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "BottomRight"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}BottomRight.png for {cleanTitle}.");
					return;
				}
				c.DrawImage(SKImage.FromEncodedData(Path.Combine(layoutPath, capitalizedDeck + "BottomRight.png")), new SKRect(0, 0, cardWidth, cardHeight));
				using (SKImage bottomRightImg = SKImage.FromEncodedData(bottomRightPath))
				{
					SKPoint bottomRightCenter = ValueFetching.GetElementPos("bottomRight");
					c.DrawImage(bottomRightImg, bottomRightCenter.X - bottomRightImg.Width / 2, bottomRightCenter.Y - bottomRightImg.Height / 2);
				}
			}

			// Ensure output directory exists and save completed card
			bool saveByDeck = ValueFetching.GetSettingsValue("Data", "exportByDeck") == "true";
			var relativeOutDir = Path.Combine("Card Data", "-Exports", saveByDeck ? deck : "");
			var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			using var outpath = File.OpenWrite(Path.Combine(outDir, $"{cleanTitle}.png"));
			b.Encode(outpath, SKEncodedImageFormat.Png, 100);
			Console.WriteLine($"Image saved: {cleanTitle}");
		}

		public static void CleanIntermediaries()
		{
			var imageIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "ImageIntermediary"));
			var textIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "TextIntermediary"));
			if (!Directory.Exists(imageIntermediaryPath)) Directory.CreateDirectory(imageIntermediaryPath);
			if (!Directory.Exists(textIntermediaryPath)) Directory.CreateDirectory(textIntermediaryPath);

			var imageIntermediaryDI = new DirectoryInfo(imageIntermediaryPath);
			foreach (FileInfo file in imageIntermediaryDI.EnumerateFiles())
			{
				if (file.Extension == ".png") file.Delete();
			}
			var textIntermediaryDI = new DirectoryInfo(textIntermediaryPath);
			foreach (FileInfo file in textIntermediaryDI.EnumerateFiles())
			{
				if (file.Extension == ".png") file.Delete();
			}
		}
	}
}
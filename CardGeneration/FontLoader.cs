using CrossPlatform_Card_Generator.CardGeneration;
using SkiaSharp;

namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class FontLoader
	{
		public static SKFont GetFont(string fontName, int size)
		{
			string fontPath = Structuring.GetFullPath(Path.Combine("fonts", fontName));
			if (!File.Exists(fontPath))
			{
				var altFont = ValueFetching.GetConfigValue("text", "altFont");
				fontPath = Structuring.GetFullPath(Path.Combine("fonts", altFont));
			}
			SKFontManager fm = SKFontManager.CreateDefault();
			SKTypeface tf = fm.CreateTypeface(fontPath);
			SKFont font = new(tf, size);
			return font;
		}
	}
}
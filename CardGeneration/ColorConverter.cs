using SkiaSharp;

namespace CrossPlatform_Card_Generator.CardGeneration
{
	public static class ColorConverter
	{
		public static SKColor FromHtml(string htmlColor)
		{
			// Remove #
			htmlColor = htmlColor[1..^0];

			byte r = (byte)Convert.ToInt32(htmlColor[0..2], 16);
			byte g = (byte)Convert.ToInt32(htmlColor[2..4], 16);
			byte b = (byte)Convert.ToInt32(htmlColor[4..6], 16);

			return new SKColor(r, g, b);
		}
	}
}
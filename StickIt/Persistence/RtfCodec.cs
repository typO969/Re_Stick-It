using System.Text;

namespace StickIt.Persistence
{
	public static class RtfCodec
	{
		public static string FromPlainText(string? text, double fontSizePt = 14.0)
		{
			var safeText = text ?? string.Empty;
			var fsHalfPoints = (int)System.Math.Round(System.Math.Max(8.0, fontSizePt) * 2.0);

			var sb = new StringBuilder();
			sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0");
			sb.Append($@"\fs{fsHalfPoints} ");

			foreach (var ch in safeText)
			{
				switch (ch)
				{
					case '\\': sb.Append(@"\\"); break;
					case '{': sb.Append(@"\{"); break;
					case '}': sb.Append(@"\}"); break;
					case '\r': break;
					case '\n': sb.Append(@"\par "); break;
					default:
						if (ch <= 0x7f)
							sb.Append(ch);
						else
							sb.Append($@"\u{(short)ch}?");
						break;
				}
			}

			sb.Append('}');
			return sb.ToString();
		}
	}
}

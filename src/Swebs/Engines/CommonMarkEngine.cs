using CommonMark;
using System.IO;
using System.Text;

namespace Swebs.Engines
{
	public class CommonMarkEngine : IEngine
	{
		public string RenderString(string template, object args = null)
		{
			return CommonMarkConverter.Convert(template);
		}

		/// <summary>
		/// Renders given file.
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public string RenderFile(string filePath, object args = null)
		{
			if (!File.Exists(filePath))
				return string.Format("Template not found: {0}", filePath);

			var sb = new StringBuilder();

			using (var reader = new StreamReader(filePath))
			using (var writer = new StringWriter(sb))
			{
				CommonMarkConverter.Convert(reader, writer);
			}

			return sb.ToString();
		}
	}
}

using System.IO;

namespace Swebs.Engines
{
	public class HtmlEngine : IEngine
	{
		public string RenderString(string template, object args = null)
		{
			return template;
		}

		public string RenderFile(string filePath, object args = null)
		{
			if (!File.Exists(filePath))
				return string.Format("Template not found: {0}", filePath);

			var contents = File.ReadAllText(filePath);
			return this.RenderString(contents, args);
		}
	}
}

using Mustache;
using System.Collections.Generic;
using System.IO;

namespace Swebs.Engines
{
	public class HandlebarsEngine : IEngine
	{
		protected FormatCompiler _compiler;
		protected Dictionary<string, Generator> _cache;

		public HandlebarsEngine()
		{
			_compiler = new FormatCompiler();
			_cache = new Dictionary<string, Generator>();

			_compiler.RemoveNewLines = false;
		}

		public string RenderString(string template, object args = null)
		{
			var generator = this.GetGenerator(template);
			var result = "";

			try
			{
				result = generator.Render(args);
			}
			catch (KeyNotFoundException ex)
			{
				result = "Error: " + ex.Message;
			}

			return result;
		}

		/// <summary>
		/// Returns generator from cache or creates and caches it.
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		protected Generator GetGenerator(string template)
		{
			lock (_cache)
			{
				if (_cache.ContainsKey(template))
					return _cache[template];

				var generator = _compiler.Compile(template);

				return (_cache[template] = generator);
			}
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

			var contents = File.ReadAllText(filePath);
			return this.RenderString(contents, args);
		}
	}
}

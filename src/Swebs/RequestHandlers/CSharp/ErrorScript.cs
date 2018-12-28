using Swebs;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Swebs.RequestHandlers.CSharp
{
	/// <summary>
	/// Script used when a C# script fails to compile.
	/// </summary>
	public class ErrorScript : Controller
	{
		private IEnumerable<Diagnostic> _errors;

		/// <summary>
		/// Creates new instance of ErrorScript for the given errors.
		/// </summary>
		/// <param name="errors"></param>
		public ErrorScript(IEnumerable<Diagnostic> errors)
		{
			_errors = errors;
		}

		/// <summary>
		/// Renders page showing the errors.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public override void Handle(HttpRequestEventArgs args, string requestPath, string localPath)
		{
			var sb = new StringBuilder();

			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html>");
			sb.AppendLine("<head>");
			sb.AppendLine("<title>Compilation failure</title>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>Error in script</h1>");
			sb.AppendFormat("<pre>");
			foreach (var error in _errors)
				sb.AppendFormat("{0}\n", error);
			sb.AppendFormat("</pre>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");

			args.Response.Send(sb.ToString());
		}
	}
}

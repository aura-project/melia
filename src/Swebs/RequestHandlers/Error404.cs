using Swebs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swebs.RequestHandlers
{
	/// <summary>
	/// Request handler for 404 errors.
	/// </summary>
	public class Error404 : IRequestHandler
	{
		/// <summary>
		/// Sends 404 page to client.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="requestPath"></param>
		/// <param name="localPath"></param>
		public void Handle(HttpRequestEventArgs args, string requestPath, string localPath)
		{
			var request = args.Request;
			var response = args.Response;

			response.StatusCode = (int)HttpStatusCode.NotFound;

			var sb = new StringBuilder();

			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html>");
			sb.AppendLine("<head>");
			sb.AppendLine("<title>Object not found</title>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<h1>Error 404 - Object not found</h1>");
			sb.AppendLine("<p>The requested URL was not found on this server.</p>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");

			using (var writer = new StreamWriter(response.OutputStream))
				writer.Write(sb.ToString());
		}
	}
}

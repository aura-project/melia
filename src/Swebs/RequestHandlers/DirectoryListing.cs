using Swebs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swebs.RequestHandlers
{
	/// <summary>
	/// Request handler for directory lists.
	/// </summary>
	public class DirectoryListing : IRequestHandler
	{
		/// <summary>
		/// Handles request by sending back a list of files and folders in
		/// the requested path.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="requestPath"></param>
		/// <param name="localPath"></param>
		public void Handle(HttpRequestEventArgs args, string requestPath, string localPath)
		{
			var request = args.Request;
			var response = args.Response;
			var server = args.Context.Server;

			response.ContentType = "text/html";

			var list = this.RenderDirectoryList(server, requestPath, localPath);
			using (var writer = new StreamWriter(response.OutputStream))
				writer.Write(list);
		}

		/// <summary>
		/// Returns directory listing page.
		/// </summary>
		/// <param name="requestPath"></param>
		/// <param name="localPath"></param>
		/// <returns></returns>
		private string RenderDirectoryList(HttpServer server, string requestPath, string localPath)
		{
			var directoryName = "/" + requestPath;
			if (string.IsNullOrWhiteSpace(directoryName))
				directoryName = "/";

			var split = requestPath.NormalizePath().Split('/');
			var backPath = "/";
			if (split.Length > 1)
				backPath += string.Join("/", split.Take(split.Length - 1));

			var sb = new StringBuilder();

			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html>");
			sb.AppendLine("<head>");
			sb.AppendLine("<title>Index of " + directoryName + "</title>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");

			{
				sb.AppendFormat("<h1>Index of {0}</h1>", directoryName);

				sb.AppendLine("<table>");

				sb.AppendLine("<tr>");
				sb.AppendFormat("<th>Name</th>");
				sb.AppendLine("<th>Last modified</th>");
				sb.AppendLine("<th>Size</th>");
				sb.AppendLine("</tr>");

				sb.AppendLine("<tr>");
				sb.AppendLine("<tr><th colspan=\"3\"><hr></th></tr>");
				sb.AppendLine("</tr>");

				sb.AppendLine("<tr>");
				sb.AppendFormat("<td><a href=\"{0}\">Parent Directory</a></td>", backPath);
				sb.AppendLine("<td></td>");
				sb.AppendLine("<td></td>");
				sb.AppendLine("</tr>");

				foreach (var filePath in server.GetLocalFoldersIn(requestPath))
				{
					var name = Path.GetFileName(filePath);
					var linkPath = "/" + Path.Combine(requestPath, name).NormalizePath();

					sb.AppendLine("<tr>");
					sb.AppendFormat("<td>[D] <a href=\"{0}\">{1}</a></td>", linkPath, name);
					sb.AppendFormat("<td>{0:yyyy-MM-dd HH:mm:ss}</td>", Directory.GetLastWriteTime(filePath));
					sb.AppendFormat("<td>-</td>");
					sb.AppendLine("</tr>");
				}

				foreach (var filePath in server.GetLocalFilesIn(requestPath))
				{
					var name = Path.GetFileName(filePath);
					var linkPath = "/" + Path.Combine(requestPath, name).NormalizePath();

					sb.AppendLine("<tr>");
					sb.AppendFormat("<td>[F] <a href=\"{0}\">{1}</a></td>", linkPath, name);
					sb.AppendFormat("<td>{0:yyyy-MM-dd HH:mm:ss}</td>", File.GetLastWriteTime(filePath));
					sb.AppendFormat("<td>{0}</td>", this.GetSizeString(filePath));
					sb.AppendLine("</tr>");
				}

				sb.AppendLine("<tr>");
				sb.AppendLine("<tr><th colspan=\"3\"><hr></th></tr>");
				sb.AppendLine("</tr>");

				sb.AppendLine("</table>");
			}

			sb.AppendLine("</body>");
			sb.AppendLine("</html>");

			return sb.ToString();
		}

		/// <summary>
		/// Returns size for the given file as string.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private string GetSizeString(string filePath)
		{
			var result = new FileInfo(filePath).Length;
			if (result < 1024)
				return result + "B";

			var resultf = result / 1024f;
			if (resultf < 1024)
				return resultf.ToString("0.##", CultureInfo.InvariantCulture) + "K";

			resultf /= 1024;
			if (resultf < 1024)
				return resultf.ToString("0.##", CultureInfo.InvariantCulture) + "M";

			resultf /= 1024;
			return result.ToString("0.##", CultureInfo.InvariantCulture) + "G";
		}
	}
}

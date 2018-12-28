using Swebs.RequestHandlers;
using Swebs.RequestHandlers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Swebs
{
	/// <summary>
	/// The web server's configuration.
	/// </summary>
	public class Configuration
	{
		/// <summary>
		/// The host to bind the server to.
		/// </summary>
		public IPAddress Host { get; set; }

		/// <summary>
		/// The port to bind the server to.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// The server's root paths.
		/// </summary>
		/// <remarks>
		/// Setting more than one path will make the server search for
		/// requested files in all of them, with the first path
		/// having the highest priority.
		/// 
		/// Since the server will handle the first valid path it finds,
		/// directory listing will only list the files from the first
		/// directory it finds.
		/// </remarks>
		public List<string> SourcePaths { get; set; }

		/// <summary>
		/// Whether to allow listing of directory contents.
		/// </summary>
		public bool AllowDirectoryListing { get; set; }

		/// <summary>
		/// List of valid index file names.
		/// </summary>
		public List<string> IndexNames { get; set; }

		/// <summary>
		/// List of special handlers for specific extensions.
		/// </summary>
		public Dictionary<string, IRequestHandler> FileTypeHandlers { get; set; }

		/// <summary>
		/// If true, server adds trailing slash to requested URLs,
		/// redirecting the client there.
		/// </summary>
		/// <example>
		/// http://127.0.0.1/test -> http://127.0.0.1/test/
		/// </example>
		public bool DirectorySlash { get; set; }

		/// <summary>
		/// Creates new Configuration instance, while setting default values
		/// for all options.
		/// </summary>
		public Configuration()
		{
			this.Host = IPAddress.Any;
			this.Port = 80;

			this.SourcePaths = new List<string>();
			this.AllowDirectoryListing = false;
			this.DirectorySlash = true;

			this.IndexNames = new List<string>();
			this.IndexNames.Add("index.cs");
			this.IndexNames.Add("index.htm");
			this.IndexNames.Add("index.html");

			this.FileTypeHandlers = new Dictionary<string, IRequestHandler>();

			this.FileTypeHandlers.Add("", new RawOutput("application/octet-stream"));

			this.FileTypeHandlers.Add(".jpg", new RawOutput("image/jpeg", "public, max-age=3600"));
			this.FileTypeHandlers.Add(".jpeg", new RawOutput("image/jpeg", "public, max-age=3600"));
			this.FileTypeHandlers.Add(".gif", new RawOutput("image/gif", "public, max-age=3600"));
			this.FileTypeHandlers.Add(".png", new RawOutput("image/png", "public, max-age=3600"));
			this.FileTypeHandlers.Add(".ico", new RawOutput("image/x-icon", "public, max-age=3600"));

			this.FileTypeHandlers.Add(".htm", new RawOutput("text/html"));
			this.FileTypeHandlers.Add(".html", new RawOutput("text/html"));
			this.FileTypeHandlers.Add(".js", new RawOutput("application/javascript"));
			this.FileTypeHandlers.Add(".json", new RawOutput("application/json"));
			this.FileTypeHandlers.Add(".css", new RawOutput("text/css"));

			this.FileTypeHandlers.Add(".xml", new RawOutput("text/xml"));
			this.FileTypeHandlers.Add(".txt", new RawOutput("text/plain"));

			this.FileTypeHandlers.Add(".cs", new CSharpScript());
		}
	}
}

using Swebs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swebs.RequestHandlers
{
	/// <summary>
	/// Default request handler for normal files.
	/// </summary>
	/// <remarks>
	/// Makes use of other request handlers, based on the handler list
	/// given in the constructor.
	/// </remarks>
	public class FileRequest : IRequestHandler
	{
		private IDictionary<string, IRequestHandler> _handlers;

		/// <summary>
		/// Creates new instance of FileRequest.
		/// </summary>
		/// <param name="handlers"></param>
		public FileRequest(IDictionary<string, IRequestHandler> handlers)
		{
			_handlers = handlers;
		}

		/// <summary>
		/// Handles request, forwarding the request to specialized handlers.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="requestPath"></param>
		/// <param name="localPath"></param>
		public void Handle(HttpRequestEventArgs args, string requestPath, string localPath)
		{
			var request = args.Request;
			var response = args.Response;
			var extension = Path.GetExtension(localPath).ToLower();
			var handler = this.GetFileTypeHandler(extension);

			handler.Handle(args, requestPath, localPath);
		}

		/// <summary>
		/// Returns the request handler for the given extension,
		/// or the default.
		/// </summary>
		/// <param name="extension"></param>
		/// <returns></returns>
		private IRequestHandler GetFileTypeHandler(string extension)
		{
			IRequestHandler result;
			if (_handlers.TryGetValue(extension, out result))
				return result;

			if (_handlers.TryGetValue("", out result))
				return result;

			_handlers.Add("", result = new RawOutput("application/octet-stream"));

			return result;
		}
	}
}

using Swebs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swebs
{
	/// <summary>
	/// Interface for request handler classes, that take requests and send
	/// a response.
	/// </summary>
	public interface IRequestHandler
	{
		void Handle(HttpRequestEventArgs args, string requestPath, string localPath);
	}
}

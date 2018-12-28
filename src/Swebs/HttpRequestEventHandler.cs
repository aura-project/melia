using System;
using System.Collections.Generic;
using System.Text;

namespace Swebs
{
	public class HttpRequestEventArgs : EventArgs
	{
		public HttpRequestEventArgs(HttpContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			Context = context;
		}

		public HttpContext Context { get; private set; }

		public HttpRequest Request { get { return Context.Request; } }

		public HttpResponse Response { get { return Context.Response; } }
	}

	public delegate void HttpRequestEventHandler(object sender, HttpRequestEventArgs e);
}

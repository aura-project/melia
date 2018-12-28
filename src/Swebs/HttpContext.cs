using System;
using System.Collections.Generic;
using System.Text;

namespace Swebs
{
	public class HttpContext
	{
		internal HttpContext(HttpServer server, HttpClient client)
		{
			Server = server;
			Request = new HttpRequest(client);
			Response = new HttpResponse(this);
		}

		public HttpServer Server { get; private set; }

		public HttpRequest Request { get; private set; }

		public HttpResponse Response { get; private set; }
	}
}

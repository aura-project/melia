using Swebs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swebs.RequestHandlers.CSharp
{
	public abstract class Controller : IRequestHandler
	{
		public abstract void Handle(HttpRequestEventArgs args, string requestPath, string localPath);
	}
}

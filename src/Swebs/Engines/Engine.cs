using System.Collections.Generic;
namespace Swebs.Engines
{
	public interface IEngine
	{
		string RenderFile(string filePath, object args = null);
		string RenderString(string template, object args = null);
	}
}

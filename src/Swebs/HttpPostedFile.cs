using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Swebs
{
	public class HttpPostedFile
	{
		/// <summary>
		/// True if a file was uploaded (FileName is not empty).
		/// </summary>
		public bool HasData { get { return !string.IsNullOrEmpty(this.FileName); } }

		public HttpPostedFile(int contentLength, string contentType, string fileName, Stream inputStream)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			if (inputStream == null)
				throw new ArgumentNullException("inputStream");

			ContentLength = contentLength;
			ContentType = contentType;
			FileName = fileName;
			InputStream = inputStream;
		}

		public int ContentLength { get; private set; }

		public string ContentType { get; private set; }

		public string FileName { get; private set; }

		public Stream InputStream { get; private set; }

		public bool MoveTo(string filePath)
		{
			var folder = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			using (var inFile = this.InputStream)
			using (var outFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				inFile.CopyTo(outFile);

			return true;
		}
	}
}

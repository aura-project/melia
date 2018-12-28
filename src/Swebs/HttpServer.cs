using Swebs.Engines;
using Swebs.RequestHandlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Swebs
{
	public class HttpServer : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpServer));

		private readonly static char[] _invalidPathCharacters = Path.GetInvalidPathChars();

		private bool _disposed;
		private TcpListener _listener;
		private readonly object _syncLock = new object();
		private readonly Dictionary<HttpClient, bool> _clients = new Dictionary<HttpClient, bool>();
		private HttpServerState _state = HttpServerState.Stopped;
		private AutoResetEvent _clientsChangedEvent = new AutoResetEvent(false);

		/// <summary>
		/// The server's currently loaded configuration.
		/// </summary>
		internal Configuration Conf { get; private set; }

		/// <summary>
		/// The server's root paths.
		/// </summary>
		internal List<string> SourcePaths { get; private set; }

		/// <summary>
		/// The request handler for normal files.
		/// </summary>
		public IRequestHandler FileAccessHandler { get; set; }

		/// <summary>
		/// The request handler for directories.
		/// </summary>
		/// <remarks>
		/// If directory listing is disabled via the configuration,
		/// Error404Handler is called instead.
		/// </remarks>
		public IRequestHandler DirectoryListingHandler { get; set; }

		/// <summary>
		/// The request handler for non-existent or invalid files.
		/// </summary>
		public IRequestHandler Error404Handler { get; set; }

		/// <summary>
		/// Collection of rendering engines.
		/// </summary>
		public Dictionary<string, IEngine> Engines { get; private set; }

		public HttpServerState State
		{
			get { return _state; }
			private set
			{
				if (_state != value)
				{
					_state = value;

					OnStateChanged(EventArgs.Empty);
				}
			}
		}

		public event HttpRequestEventHandler RequestReceived;

		protected virtual void OnRequestReceived(HttpRequestEventArgs e)
		{
			foreach (HttpRequestEventHandler handler in RequestReceived.GetInvocationList())
			{
				try
				{
					handler.Invoke(this, e);
				}
				catch (Exception ex)
				{
					ex.Source = handler.Method.Module + "/" + handler.Method.DeclaringType.Name + "/" + handler.Method.Name;
					RaiseUnhandledException(e.Context, ex);
				}
			}
		}

		public event HttpExceptionEventHandler UnhandledException;

		protected virtual void OnUnhandledException(HttpExceptionEventArgs e)
		{
			var ev = UnhandledException;

			if (ev != null)
				ev(this, e);
		}

		public event EventHandler StateChanged;

		protected virtual void OnStateChanged(EventArgs e)
		{
			var ev = StateChanged;

			if (ev != null)
				ev(this, e);
		}

		public IPEndPoint EndPoint { get; set; }

		public int ReadBufferSize { get; set; }

		public int WriteBufferSize { get; set; }

		public string ServerBanner { get; set; }

		public TimeSpan ReadTimeout { get; set; }

		public TimeSpan WriteTimeout { get; set; }

		public TimeSpan ShutdownTimeout { get; set; }

		internal HttpTimeoutManager TimeoutManager { get; private set; }

		public HttpServer(Configuration conf)
		{
			this.Conf = conf;
			this.SourcePaths = this.Conf.SourcePaths.Select(a => Path.GetFullPath(a).NormalizePath()).ToList();

			this.FileAccessHandler = new FileRequest(this.Conf.FileTypeHandlers.ToDictionary(a => a.Key, b => b.Value));
			this.DirectoryListingHandler = new DirectoryListing();
			this.Error404Handler = new Error404();

			this.Engines = new Dictionary<string, IEngine>();
			this.Engines.Add("html", new HtmlEngine());
			this.Engines.Add("hbs", new HandlebarsEngine());
			this.Engines.Add("md", new CommonMarkEngine());

			EndPoint = new IPEndPoint(this.Conf.Host, this.Conf.Port);
			RequestReceived += this.OnRequestReceived;

			ReadBufferSize = 4096;
			WriteBufferSize = 4096;
			ShutdownTimeout = TimeSpan.FromSeconds(30);
			ReadTimeout = TimeSpan.FromSeconds(90);
			WriteTimeout = TimeSpan.FromSeconds(90);

			ServerBanner = String.Format("NHttp/{0}", GetType().Assembly.GetName().Version);
		}

		public void Start()
		{
			VerifyState(HttpServerState.Stopped);

			State = HttpServerState.Starting;

			Log.Debug(String.Format("Starting HTTP server at {0}", EndPoint));

			TimeoutManager = new HttpTimeoutManager(this);

			// Start the listener.

			var listener = new TcpListener(EndPoint);

			try
			{
				listener.Start();

				EndPoint = (IPEndPoint)listener.LocalEndpoint;

				_listener = listener;

				Log.Info(String.Format("HTTP server running at {0}", EndPoint));
			}
			catch (Exception ex)
			{
				State = HttpServerState.Stopped;

				Log.Error("Failed to start HTTP server", ex);

				throw new NHttpException("Failed to start HTTP server", ex);
			}

			State = HttpServerState.Started;

			BeginAcceptTcpClient();
		}

		public void Stop()
		{
			VerifyState(HttpServerState.Started);

			Log.Debug("Stopping HTTP server");

			State = HttpServerState.Stopping;

			try
			{
				// Prevent any new connections.

				_listener.Stop();

				// Wait for all clients to complete.

				StopClients();
			}
			catch (Exception ex)
			{
				Log.Error("Failed to stop HTTP server", ex);

				throw new NHttpException("Failed to stop HTTP server", ex);
			}
			finally
			{
				_listener = null;

				State = HttpServerState.Stopped;

				Log.Info("Stopped HTTP server");
			}
		}

		private void StopClients()
		{
			var shutdownStarted = DateTime.Now;
			bool forceShutdown = false;

			// Clients that are waiting for new requests are closed.

			List<HttpClient> clients;

			lock (_syncLock)
			{
				clients = new List<HttpClient>(_clients.Keys);
			}

			foreach (var client in clients)
			{
				client.RequestClose();
			}

			// First give all clients a chance to complete their running requests.

			while (true)
			{
				lock (_syncLock)
				{
					if (_clients.Count == 0)
						break;
				}

				var shutdownRunning = DateTime.Now - shutdownStarted;

				if (shutdownRunning >= ShutdownTimeout)
				{
					forceShutdown = true;
					break;
				}

				_clientsChangedEvent.WaitOne(ShutdownTimeout - shutdownRunning);
			}

			if (!forceShutdown)
				return;

			// If there are still clients running after the timeout, their
			// connections will be forcibly closed.

			lock (_syncLock)
			{
				clients = new List<HttpClient>(_clients.Keys);
			}

			foreach (var client in clients)
			{
				client.ForceClose();
			}

			// Wait for the registered clients to be cleared.

			while (true)
			{
				lock (_syncLock)
				{
					if (_clients.Count == 0)
						break;
				}

				_clientsChangedEvent.WaitOne();
			}
		}

		private void BeginAcceptTcpClient()
		{
			var listener = _listener;
			if (listener != null)
				listener.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
		}

		private void AcceptTcpClientCallback(IAsyncResult asyncResult)
		{
			try
			{
				var listener = _listener; // Prevent race condition.

				if (listener == null)
					return;

				var tcpClient = listener.EndAcceptTcpClient(asyncResult);

				// If we've stopped already, close the TCP client now.

				if (_state != HttpServerState.Started)
				{
					tcpClient.Close();
					return;
				}

				var client = new HttpClient(this, tcpClient);

				RegisterClient(client);

				client.BeginRequest();

				BeginAcceptTcpClient();
			}
			catch (ObjectDisposedException)
			{
				// EndAcceptTcpClient will throw a ObjectDisposedException
				// when we're shutting down. This can safely be ignored.
			}
			catch (Exception ex)
			{
				Log.Info("Failed to accept TCP client", ex);
			}
		}

		private void RegisterClient(HttpClient client)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			lock (_syncLock)
			{
				_clients.Add(client, true);

				_clientsChangedEvent.Set();
			}
		}

		internal void UnregisterClient(HttpClient client)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			lock (_syncLock)
			{
				Debug.Assert(_clients.ContainsKey(client));

				_clients.Remove(client);

				_clientsChangedEvent.Set();
			}
		}

		private void VerifyState(HttpServerState state)
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().Name);
			if (_state != state)
				throw new InvalidOperationException(String.Format("Expected server to be in the '{0}' state", state));
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				if (_state == HttpServerState.Started)
					Stop();

				if (_clientsChangedEvent != null)
				{
					((IDisposable)_clientsChangedEvent).Dispose();
					_clientsChangedEvent = null;
				}

				if (TimeoutManager != null)
				{
					TimeoutManager.Dispose();
					TimeoutManager = null;
				}

				_disposed = true;
			}
		}

		internal void RaiseRequest(HttpContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			OnRequestReceived(new HttpRequestEventArgs(context));
		}

		internal bool RaiseUnhandledException(HttpContext context, Exception exception)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			var e = new HttpExceptionEventArgs(context, exception);

			OnUnhandledException(e);

			return e.Handled;
		}

		/// <summary>
		/// Called when a request comes in.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void OnRequestReceived(object sender, HttpRequestEventArgs args)
		{
			var path = args.Request.Path;

			var requestPath = path.NormalizePath();
			requestPath = HttpUtil.UriDecode(requestPath);
			requestPath = requestPath.Trim('/');

			// Check for invalid characters in path
			if (requestPath.IndexOfAny(_invalidPathCharacters) != -1)
			{
				Log.Warn("Invalid path (chr) '{0}' requested by '{1}:{2}'.", path, args.Request.ClientIp, args.Request.ClientPort);
				args.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				//Console.WriteLine("Invalid: " + path);
				return;
			}

			// Check validity of path
			// It would probably be better not to use Path, since it's not
			// designed with URLs in mind, but for now this will prevent
			// unhandled exceptions because Path couldn't handle an "invalid"
			// character like a colon.
			try
			{
				if (!string.IsNullOrWhiteSpace(requestPath))
					Path.GetFullPath(requestPath);
			}
			catch (NotSupportedException)
			{
				Log.Warn("Invalid path (ex) '{0}' requested by '{1}:{2}'.", path, args.Request.ClientIp, args.Request.ClientPort);
				args.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				//Console.WriteLine("Invalid: " + path);
				return;
			}

			foreach (var rootPath in this.SourcePaths)
			{
				var localPath = Path.Combine(rootPath, requestPath);
				localPath = HttpUtil.UriDecode(localPath);
				localPath = localPath.NormalizePath();

				// Check scope
				var fullRequestPath = Path.GetFullPath(localPath).NormalizePath().TrimEnd('/');
				var fullRootPath = Path.GetFullPath(rootPath).NormalizePath().TrimEnd('/');
				if (!fullRequestPath.StartsWith(fullRootPath))
				{
					//this.Error404Handler.Handle(args, requestPath, localPath);
					//return;
					continue;
				}

				// Handle request
				if (File.Exists(localPath))
				{
					this.FileAccessHandler.Handle(args, requestPath, localPath);
					return;
				}
				else if (Directory.Exists(localPath))
				{
					if (this.Conf.DirectorySlash && !path.EndsWith("/"))
					{
						args.Response.Redirect(path + "/");
						return;
					}

					// Check index files
					if (this.TestIndexNames(this.Conf.IndexNames, ref localPath))
					{
						this.FileAccessHandler.Handle(args, requestPath, localPath);
						return;
					}

					// List directory contents
					if (this.Conf.AllowDirectoryListing)
					{
						this.DirectoryListingHandler.Handle(args, requestPath, localPath);
						return;
					}
				}
			}

			this.Error404Handler.Handle(args, requestPath, null);
		}

		/// <summary>
		/// Returns path to the first local file or folder found in the
		/// source paths. Returns null if none exist.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string GetLocalPath(string path)
		{
			var requestPath = path.NormalizePath();
			requestPath = HttpUtil.UriDecode(requestPath);
			requestPath = requestPath.Trim('/');

			foreach (var rootPath in this.SourcePaths)
			{
				var localPath = Path.Combine(rootPath, requestPath);
				localPath = HttpUtil.UriDecode(localPath);
				localPath = localPath.NormalizePath();

				// Check scope
				var fullRequestPath = Path.GetFullPath(localPath).NormalizePath().TrimEnd('/');
				var fullRootPath = Path.GetFullPath(rootPath).NormalizePath().TrimEnd('/');
				if (!fullRequestPath.StartsWith(fullRootPath))
					continue;

				if (File.Exists(localPath) || Directory.Exists(localPath))
					return localPath;
			}

			return null;
		}

		/// <summary>
		/// Returns paths to all files in given folder, relative to all
		/// source folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string[] GetLocalFilesIn(string path)
		{
			return this.EnumerateIn(path, Directory.EnumerateFiles);
		}

		/// <summary>
		/// Returns paths to all folders in given folder, relative to all
		/// source folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string[] GetLocalFoldersIn(string path)
		{
			return this.EnumerateIn(path, Directory.EnumerateDirectories);
		}

		/// <summary>
		/// Returns paths to all files in given folder, relative to all
		/// source folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string[] EnumerateIn(string path, Func<string, IEnumerable<string>> enumFunc)
		{
			var requestPath = path.NormalizePath();
			requestPath = HttpUtil.UriDecode(requestPath);
			requestPath = requestPath.Trim('/');

			var result = new Dictionary<string, string>();

			foreach (var rootPath in this.SourcePaths)
			{
				var localPath = Path.Combine(rootPath, requestPath);
				localPath = HttpUtil.UriDecode(localPath);
				localPath = localPath.NormalizePath();

				// Check scope
				var fullRequestPath = Path.GetFullPath(localPath).NormalizePath().TrimEnd('/');
				var fullRootPath = Path.GetFullPath(rootPath).NormalizePath().TrimEnd('/');
				if (!fullRequestPath.StartsWith(fullRootPath))
					continue;

				if (!Directory.Exists(localPath))
					continue;

				foreach (var filePath in enumFunc(localPath))
				{
					var fileName = Path.GetFileName(filePath);
					var relativePath = Path.Combine(requestPath, fileName).NormalizePath();

					if (!result.ContainsKey(relativePath))
						result[relativePath] = filePath.NormalizePath();
				}
			}

			return result.Values.ToArray();
		}

		/// <summary>
		/// Checks given path is a directory that contains an index file.
		/// If so, the path is set to that index file's path and true
		/// is returned.
		/// </summary>
		/// <param name="names"></param>
		/// <param name="localPath"></param>
		/// <returns></returns>
		private bool TestIndexNames(IList<string> names, ref string localPath)
		{
			if (Directory.Exists(localPath))
			{
				foreach (var name in this.Conf.IndexNames)
				{
					var indexPath = Path.Combine(localPath, name).Normalize();
					if (File.Exists(indexPath))
					{
						localPath = indexPath;
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns string with given name, or null if no engine was set for
		/// the name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IEngine GetEngine(string name)
		{
			IEngine result;
			this.Engines.TryGetValue(name, out result);
			return result;
		}
	}

	public static class StringExtension
	{
		/// <summary>
		/// Replaces backward-slashes with forward-slashes.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string NormalizePath(this string path)
		{
			return path.Replace('\\', '/');
		}
	}
}

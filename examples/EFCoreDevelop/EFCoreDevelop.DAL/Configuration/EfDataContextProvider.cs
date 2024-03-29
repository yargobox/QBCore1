using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using QBCore.Configuration;
using QBCore.DataSource;

namespace Develop.DAL.Configuration;

public sealed class EfDataContextProvider : IEfCoreDataContextProvider, IDesignTimeDbContextFactory<DbDevelopContext>
{
	private const string _defaultDataContextName = "default";
	private const string _appSettingsRelFilePath = "EFCoreDevelop.API/appsettings.json";

	private OptionsListener<SqlDbSettings>? _listener;
	private SqlDbSettings? _settings;
	private IEfCoreDataContext? _dataContext;
	public bool IsDisposed { get; private set; }

	public IEnumerable<DataContextInfo> Infos
	{
		get
		{
			yield return new DataContextInfo(_defaultDataContextName, () => EfCoreDataLayer.Default);
		}
	}

	public EfDataContextProvider()
	{
	}

/* 	public EfDataContextProvider(IOptions<SqlDbSettings> options)
	{
		if (options == null) throw new ArgumentNullException(nameof(options));

		_settings = options.Value;
	} */

	public EfDataContextProvider(OptionsListener<SqlDbSettings> optionsListener)
	{
		if (optionsListener == null) throw new ArgumentNullException(nameof(optionsListener));

		_listener = optionsListener;
	}

	public IDataContext GetDataContext(string dataContextName = _defaultDataContextName)
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(EfDataContextProvider));

		if (dataContextName != _defaultDataContextName) throw new InvalidOperationException($"Unknown data context '{dataContextName}'.");

		if (_dataContext == null)
		{
			if (_listener != null || _settings != null)
			{
				var setup = new DbContextOptionsBuilder<DbDevelopContext>();
				setup.UseNpgsql((_listener?.Value1 ?? _settings!).ConnectionString());
				var dbDevelopContext = new DbDevelopContext(setup.Options);
				_dataContext = new EfCoreDataContext(dbDevelopContext, dataContextName);
			}
			else
			{
				var dbDevelopContext = CreateDbContext(null!);
				_dataContext = new EfCoreDataContext(dbDevelopContext, dataContextName);
			}
		}

		return _dataContext;
	}

	public DbContext CreateModelOrientedDbContext(string dataContextName = "default")
	{
		if (dataContextName != _defaultDataContextName) throw new InvalidOperationException($"Unknown data context '{dataContextName}'.");

		var setup = new DbContextOptionsBuilder<DbDevelopContext>();
		setup.UseNpgsql(string.Empty);
		DbContext dbDevelopContext = new DbDevelopContext(setup.Options);
		return dbDevelopContext;
	}

	/// <summary>
	/// Creates a database context initialized with settings read directly from the appsettings.json file
	/// if the provider has been created by a parameterless constructor.
	/// </summary>
	/// <param name="args">Optional arguments</param>
	/// <remarks>The caller is responsible for disposing the returned database context.</remarks>
	/// <exception cref="ObjectDisposedException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public DbDevelopContext CreateDbContext(string[] args)
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(EfDataContextProvider));

		if (_listener == null && _settings == null)
		{
			var filePath = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory)!, _appSettingsRelFilePath);
			var configurationBuilder = new ConfigurationBuilder();
			configurationBuilder.AddJsonFile(filePath);
			var configurationRoot = configurationBuilder.Build();
			_settings = configurationRoot.GetSection(nameof(SqlDbSettings)).Get<SqlDbSettings>()
				?? throw new InvalidOperationException($"Database context settings '{nameof(SqlDbSettings)}' is not set in the file '{filePath}'.");
		}

		var setup = new DbContextOptionsBuilder<DbDevelopContext>();
		setup.UseNpgsql((_listener?.Value1 ?? _settings!).ConnectionString());
		return new DbDevelopContext(setup.Options);
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;

			var disposable = _dataContext?.Context as IDisposable;

			_listener = null;
			_settings = null;
			_dataContext = null;

			disposable?.Dispose();
		}
	}
}
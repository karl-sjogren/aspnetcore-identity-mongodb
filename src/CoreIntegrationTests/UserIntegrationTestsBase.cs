namespace IntegrationTests
{
	using System;
	using Microsoft.AspNetCore.Identity;
	using Microsoft.AspNetCore.Identity.MongoDB;
	using Microsoft.Extensions.DependencyInjection;
    using Mongo2Go;
    using MongoDB.Bson.Serialization.Conventions;
    using MongoDB.Driver;
	using NUnit.Framework;

	public class UserIntegrationTestsBase : AssertionHelper
	{
    	private static MongoDbRunner _runner;
		internal static MongoDbRunner Runner => _runner ?? (_runner = MongoDbRunner.Start());
		protected MongoDatabase Database;
		protected MongoCollection<IdentityUser> Users;
		protected MongoCollection<IdentityRole> Roles;

		// note: for now we'll have interfaces to both the new and old apis for MongoDB, that way we don't have to update all the tests at once and risk introducing bugs
		protected IMongoDatabase DatabaseNewApi;
		protected IServiceProvider ServiceProvider;
		private const string IdentityTesting = "identity-testing";

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient(Runner.ConnectionString + IdentityTesting);

			// todo move away from GetServer which could be deprecated at some point
			Database = client.GetServer().GetDatabase(IdentityTesting);
			Users = Database.GetCollection<IdentityUser>("users");
			Roles = Database.GetCollection<IdentityRole>("roles");
			
			var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        	ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

			DatabaseNewApi = client.GetDatabase(IdentityTesting);

			Database.DropCollection("users");
			Database.DropCollection("roles");

			ServiceProvider = CreateServiceProvider<IdentityUser, IdentityRole>();
		}

		protected UserManager<IdentityUser> GetUserManager()
			=> ServiceProvider.GetService<UserManager<IdentityUser>>();

		protected RoleManager<IdentityRole> GetRoleManager()
			=> ServiceProvider.GetService<RoleManager<IdentityRole>>();

		protected IServiceProvider CreateServiceProvider<TUser, TRole>(Action<IdentityOptions> optionsProvider = null)
			where TUser : IdentityUser
			where TRole : IdentityRole
		{
			var services = new ServiceCollection();
			optionsProvider = optionsProvider ?? (options => { });
			services.AddIdentity<TUser, TRole>(optionsProvider)
				.AddDefaultTokenProviders()
				.RegisterMongoStores<TUser, TRole>(Runner.ConnectionString + IdentityTesting);

			services.AddLogging();

			return services.BuildServiceProvider();
		}
	}
}
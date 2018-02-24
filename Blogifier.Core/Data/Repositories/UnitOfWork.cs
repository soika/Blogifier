using Blogifier.Core.Data.Interfaces;

namespace Blogifier.Core.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BlogifierDbContext db;

        public UnitOfWork(BlogifierDbContext db)
        {
            this.db = db;
            Assets = new AssetRepository(this.db);
            Profiles = new ProfileRepository(this.db);
            Categories = new CategoryRepository(this.db);
            BlogPosts = new PostRepository(this.db);
            CustomFields = new CustomRepository(this.db);
        }

        public IAssetRepository Assets { get; private set; }
        public IProfileRepository Profiles { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IPostRepository BlogPosts { get; private set; }
        public ICustomRepository CustomFields { get; private set; }

        public int Complete()
        {
            return this.db.SaveChanges();
        }

        public void Dispose()
        {
            this.db.Dispose();
        }
    }
}

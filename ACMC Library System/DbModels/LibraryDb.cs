using System.Data.Entity;
namespace ACMC_Library_System.DbModels
{
    public class LibraryDb : DbContext
    {
        public LibraryDb() : base(Properties.Settings.Default.ConnectionString)
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
            Database.CommandTimeout = 10;
        }

        public virtual DbSet<action_history> action_history { get; set; }
        public virtual DbSet<action_type> action_type { get; set; }
        public virtual DbSet<issue> issue { get; set; }
        public virtual DbSet<item> item { get; set; }
        public virtual DbSet<item_category> item_category { get; set; }
        public virtual DbSet<item_class> item_class { get; set; }
        public virtual DbSet<item_status> item_status { get; set; }
        public virtual DbSet<patron> patron { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<action_type>()
                .HasMany(e => e.action_history)
                .WithRequired(e => e.action_type1)
                .HasForeignKey(e => e.action_type)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<item>()
                .Property(e => e.moreinfo)
                .IsUnicode(false);

            modelBuilder.Entity<item>()
                .HasMany(e => e.issue)
                .WithRequired(e => e.item)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<item_category>()
                .Property(e => e.cat_code)
                .IsUnicode(false);

            modelBuilder.Entity<item_category>()
                .HasMany(e => e.item)
                .WithOptional(e => e.item_category)
                .HasForeignKey(e => e.category);

            modelBuilder.Entity<item_class>()
                .HasMany(e => e.item)
                .WithOptional(e => e.item_class)
                .HasForeignKey(e => e.item_subclass);

            modelBuilder.Entity<patron>()
                .Property(e => e.balance)
                .HasPrecision(19, 4);

            modelBuilder.Entity<patron>()
                .HasMany(e => e.patron1)
                .WithOptional(e => e.patron2)
                .HasForeignKey(e => e.guarantor);
        }
    }
}

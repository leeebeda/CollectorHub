using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class DBContext : DbContext
{
    public DBContext()
    {
    }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<CollectionField> CollectionFields { get; set; }

    public virtual DbSet<CollectionImage> CollectionImages { get; set; }

    public virtual DbSet<FieldOption> FieldOptions { get; set; }

    public virtual DbSet<FieldType> FieldTypes { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemStatus> ItemStatuses { get; set; }

    public virtual DbSet<ItemValueBool> ItemValueBools { get; set; }

    public virtual DbSet<ItemValueDate> ItemValueDates { get; set; }

    public virtual DbSet<ItemValueImage> ItemValueImages { get; set; }

    public virtual DbSet<ItemValueNumber> ItemValueNumbers { get; set; }

    public virtual DbSet<ItemValueOption> ItemValueOptions { get; set; }

    public virtual DbSet<ItemValueText> ItemValueTexts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<TagAssignment> TagAssignments { get; set; }

    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<TemplateField> TemplateFields { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VisibilityType> VisibilityTypes { get; set; }

    public virtual DbSet<WishlistItem> WishlistItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=NIKAPC;Database=CollectionManager;User=user;Password=user;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.collection_id).HasName("PK__Collecti__53D3A5CA81B6E60C");

            entity.HasOne(d => d.parent).WithMany(p => p.Inverseparent).HasConstraintName("FK__Collectio__paren__30F848ED");

            entity.HasOne(d => d.template).WithMany(p => p.Collections).HasConstraintName("FK__Collectio__templ__2F10007B");

            entity.HasOne(d => d.user).WithMany(p => p.Collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collectio__user___2E1BDC42");

            entity.HasOne(d => d.visibility).WithMany(p => p.Collections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collectio__visib__300424B4");
        });

        modelBuilder.Entity<CollectionField>(entity =>
        {
            entity.HasKey(e => e.field_id).HasName("PK__Collecti__1BB6F43E0F2ED02A");

            entity.HasOne(d => d.collection).WithMany(p => p.CollectionFields)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collectio__colle__398D8EEE");

            entity.HasOne(d => d.field_type).WithMany(p => p.CollectionFields)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collectio__field__3A81B327");
        });

        modelBuilder.Entity<CollectionImage>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Collecti__3213E83FC903E117");

            entity.HasOne(d => d.collection).WithMany(p => p.CollectionImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Collectio__colle__6754599E");
        });

        modelBuilder.Entity<FieldOption>(entity =>
        {
            entity.HasKey(e => e.option_id).HasName("PK__FieldOpt__F4EACE1B00CDD967");

            entity.HasOne(d => d.field).WithMany(p => p.FieldOptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FieldOpti__field__5629CD9C");
        });

        modelBuilder.Entity<FieldType>(entity =>
        {
            entity.HasKey(e => e.field_type_id).HasName("PK__FieldTyp__1AC5A498D0DACF95");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.item_id).HasName("PK__Items__52020FDDB40B3195");

            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.collection).WithMany(p => p.Items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Items__collectio__403A8C7D");

            entity.HasOne(d => d.status).WithMany(p => p.Items)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Items__status_id__412EB0B6");
        });

        modelBuilder.Entity<ItemStatus>(entity =>
        {
            entity.HasKey(e => e.status_id).HasName("PK__ItemStat__3683B5314220965F");
        });

        modelBuilder.Entity<ItemValueBool>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83F0C17730A");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueBools)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__534D60F1");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueBools)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___52593CB8");
        });

        modelBuilder.Entity<ItemValueDate>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83F14709B58");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueDates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__4F7CD00D");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueDates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___4E88ABD4");
        });

        modelBuilder.Entity<ItemValueImage>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83F7242D337");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__5EBF139D");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___5DCAEF64");
        });

        modelBuilder.Entity<ItemValueNumber>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83F7708AABD");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueNumbers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__4BAC3F29");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueNumbers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___4AB81AF0");
        });

        modelBuilder.Entity<ItemValueOption>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83FE6945D66");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueOptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__59FA5E80");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueOptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___59063A47");

            entity.HasOne(d => d.option).WithMany(p => p.ItemValueOptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__optio__5AEE82B9");
        });

        modelBuilder.Entity<ItemValueText>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ItemValu__3213E83FA26E42EC");

            entity.HasOne(d => d.field).WithMany(p => p.ItemValueTexts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__field__47DBAE45");

            entity.HasOne(d => d.item).WithMany(p => p.ItemValueTexts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ItemValue__item___46E78A0C");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.role_id).HasName("PK__Roles__760965CC313312E6");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.tag_id).HasName("PK__Tags__4296A2B6921BA059");

            entity.HasOne(d => d.user).WithMany(p => p.Tags)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tags__user_id__619B8048");
        });

        modelBuilder.Entity<TagAssignment>(entity =>
        {
            entity.HasKey(e => new { e.tag_id, e.target_type, e.target_id }).HasName("PK__TagAssig__CDC974FB5FA7BD9B");

            entity.HasOne(d => d.tag).WithMany(p => p.TagAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TagAssign__tag_i__6477ECF3");
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.template_id).HasName("PK__Template__BE44E07943A55D69");
        });

        modelBuilder.Entity<TemplateField>(entity =>
        {
            entity.HasKey(e => e.field_id).HasName("PK__Template__1BB6F43EC6CADDB8");

            entity.HasOne(d => d.field_type).WithMany(p => p.TemplateFields)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TemplateF__field__36B12243");

            entity.HasOne(d => d.template).WithMany(p => p.TemplateFields)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TemplateF__templ__35BCFE0A");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.user_id).HasName("PK__Users__B9BE370F445554C4");

            entity.HasOne(d => d.role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__role_id__276EDEB3");
        });

        modelBuilder.Entity<VisibilityType>(entity =>
        {
            entity.HasKey(e => e.visibility_id).HasName("PK__Visibili__22EA152BBEC922DC");
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.item_id).HasName("PK__Wishlist__52020FDDE662300C");

            entity.Property(e => e.item_id).ValueGeneratedNever();

            entity.HasOne(d => d.item).WithOne(p => p.WishlistItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WishlistI__item___440B1D61");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

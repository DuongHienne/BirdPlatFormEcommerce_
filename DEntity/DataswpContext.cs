using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BirdPlatFormEcommerce.DEntity;

public partial class DataswpContext : DbContext
{
    public DataswpContext()
    {
    }

    public DataswpContext(DbContextOptions<DataswpContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbAddressReceive> TbAddressReceives { get; set; }

    public virtual DbSet<TbCart> TbCarts { get; set; }

    public virtual DbSet<TbFeedback> TbFeedbacks { get; set; }

    public virtual DbSet<TbFeedbackImage> TbFeedbackImages { get; set; }

    public virtual DbSet<TbImage> TbImages { get; set; }

    public virtual DbSet<TbOrder> TbOrders { get; set; }

    public virtual DbSet<TbOrderDetail> TbOrderDetails { get; set; }

    public virtual DbSet<TbPayment> TbPayments { get; set; }

    public virtual DbSet<TbPost> TbPosts { get; set; }

    public virtual DbSet<TbProduct> TbProducts { get; set; }

    public virtual DbSet<TbProductCategory> TbProductCategories { get; set; }

    public virtual DbSet<TbRole> TbRoles { get; set; }

    public virtual DbSet<TbShop> TbShops { get; set; }

    public virtual DbSet<TbToken> TbTokens { get; set; }

    public virtual DbSet<TbUser> TbUsers { get; set; }

    public virtual DbSet<TbWishList> TbWishLists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=ADMIN-PC\\SQLEXPRESS;Initial Catalog=dataswp;Integrated Security=True;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbAddressReceive>(entity =>
        {
            entity.HasKey(e => e.AddressId);

            entity.ToTable("tb_AddressReceive");

            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AddressDetail).HasColumnType("ntext");
            entity.Property(e => e.NameRg).HasMaxLength(250);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.TbAddressReceives)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_AddressReceive_tb_User");
        });

        modelBuilder.Entity<TbCart>(entity =>
        {
            entity.ToTable("tb_Cart");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ShopName).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.TbCarts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Cart_tb_Product");

            entity.HasOne(d => d.User).WithMany(p => p.TbCarts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Cart_tb_User");
        });

        modelBuilder.Entity<TbFeedback>(entity =>
        {
            entity.ToTable("tb_Feedback");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Detail).HasColumnType("ntext");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.TbFeedbacks)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Feedback_tb_Product");

            entity.HasOne(d => d.User).WithMany(p => p.TbFeedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Feedback_tb_User");
        });

        modelBuilder.Entity<TbFeedbackImage>(entity =>
        {
            entity.HasKey(e => e.FbImgId);

            entity.ToTable("tb_FeedbackImage");

            entity.Property(e => e.FbImgId).HasColumnName("FbImgID");
            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.ImagePath).IsUnicode(false);

            entity.HasOne(d => d.Feedback).WithMany(p => p.TbFeedbackImages)
                .HasForeignKey(d => d.FeedbackId)
                .HasConstraintName("FK_tb_FeedbackImage_tb_Feedback");
        });

        modelBuilder.Entity<TbImage>(entity =>
        {
            entity.ToTable("tb_Image");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.ImagePath).IsUnicode(false);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.VideoPath).IsUnicode(false);

            entity.HasOne(d => d.Product).WithMany(p => p.TbImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Image_tb_Product");
        });

        modelBuilder.Entity<TbOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.ToTable("tb_Order");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AddressId).HasColumnName("AddressID");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.ShopId).HasColumnName("ShopId");

            entity.HasOne(d => d.Address).WithMany(p => p.TbOrders)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Order_tb_AddressReceive");

            entity.HasOne(d => d.Payment).WithMany(p => p.TbOrders)
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("FK_tb_Order_tb_Payment");

            entity.HasOne(d => d.Shop).WithMany(p => p.TbOrders)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Order_tb_Shop");

            entity.HasOne(d => d.User).WithMany(p => p.TbOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Order_tb_User");
        });

        modelBuilder.Entity<TbOrderDetail>(entity =>
        {
            entity.ToTable("tb_OrderDetail");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DiscountPrice).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductPrice).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Order).WithMany(p => p.TbOrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_OrderDetail_tb_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.TbOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_OrderDetail_tb_Product");
        });

        modelBuilder.Entity<TbPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.ToTable("tb_Payment");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.TbPayments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Payment_tb_User");
        });

        modelBuilder.Entity<TbPost>(entity =>
        {
            entity.ToTable("tb_Post");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreateBy).HasMaxLength(250);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasOne(d => d.Product).WithMany(p => p.TbPosts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Post_tb_Product");

            entity.HasOne(d => d.Shop).WithMany(p => p.TbPosts)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Post_tb_Shop");
        });

        modelBuilder.Entity<TbProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.ToTable("tb_Product");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CateId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CateID");
            entity.Property(e => e.Decription).HasMaxLength(500);
            entity.Property(e => e.Detail).HasColumnType("ntext");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.SoldPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasDefaultValueSql("((1))");
            entity.Property(e => e.Thumbnail).IsUnicode(false);

            entity.HasOne(d => d.Cate).WithMany(p => p.TbProducts)
                .HasForeignKey(d => d.CateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Product_tb_ProductCategory");

            entity.HasOne(d => d.Shop).WithMany(p => p.TbProducts)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK_tb_Product_tb_Shop");
        });

        modelBuilder.Entity<TbProductCategory>(entity =>
        {
            entity.HasKey(e => e.CateId);

            entity.ToTable("tb_ProductCategory");

            entity.Property(e => e.CateId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CateID");
            entity.Property(e => e.CateName).HasMaxLength(50);
        });

        modelBuilder.Entity<TbRole>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.ToTable("tb_Role");

            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<TbShop>(entity =>
        {
            entity.HasKey(e => e.ShopId);

            entity.ToTable("tb_Shop");

            entity.Property(e => e.ShopId).HasColumnName("ShopID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.IsVerified).HasColumnName("Is_verified");
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.ShopName).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.TbShops)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_tb_Shop_tb_User1");
        });

        modelBuilder.Entity<TbToken>(entity =>
        {
            entity.ToTable("tb_Token");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Token).HasColumnType("ntext");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.TbTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_Token_tb_User");
        });

        modelBuilder.Entity<TbUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("tb_User");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Avatar).IsUnicode(false);
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Dob).HasColumnType("date");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.IsShop).HasColumnName("isShop");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .HasColumnName("RoleID");
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.TbUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_User_tb_Role");
        });

        modelBuilder.Entity<TbWishList>(entity =>
        {
            entity.ToTable("tb_WishList");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.TbWishLists)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_WishList_tb_Product");

            entity.HasOne(d => d.User).WithMany(p => p.TbWishLists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tb_WishList_tb_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

namespace ACMC_Library_System.DbModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using DomainModels.DataModel;

    [Table("item")]
    public partial class item
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public item()
        {
            issue = new HashSet<issue>();
        }

        public int id { get; set; }

        [StringLength(30)]
        public string code { get; set; }

        [StringLength(100)]
        public string barcode { get; set; }

        [StringLength(100)]
        public string title { get; set; }

        [StringLength(100)]
        public string keywords { get; set; }

        [StringLength(100)]
        public string publisher { get; set; }

        [StringLength(100)]
        public string description { get; set; }

        [Column(TypeName = "text")]
        public string moreinfo { get; set; }

        public int? base_location { get; set; }

        public int? current_location { get; set; }

        public int? category { get; set; }

        public int? item_subclass { get; set; }

        [StringLength(100)]
        public string author { get; set; }

        [StringLength(100)]
        public string isbn { get; set; }

        public int? pages { get; set; }

        public int? minutes { get; set; }

        public int? status { get; set; }

        public int? patronid { get; set; }

        [StringLength(100)]
        public string translator { get; set; }

        [StringLength(100)]
        public string language { get; set; }

        [StringLength(100)]
        public string donator { get; set; }

        [StringLength(100)]
        public string price { get; set; }

        [StringLength(100)]
        public string published_date { get; set; }

        public DateTime? due_date { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<issue> issue { get; set; }

        public virtual item_category item_category { get; set; }

        public virtual item_class item_class { get; set; }

        #region Extend property

        [NotMapped]
        public bool IsOverDued => due_date < DateTime.Today;

        [NotMapped]
        public patron Borrower { get; set; }

        [NotMapped]
        public bool HasBorrower => patronid != null;

        [NotMapped]
        public double Fine => due_date == null || due_date > DateTime.Today ? 0 : Math.Ceiling(DateTime.Today.Subtract(due_date.GetValueOrDefault()).Days / 7d) * BusinessRules.FinesPerWeek;

        #endregion
    }
}

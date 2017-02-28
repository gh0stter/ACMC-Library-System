using System.Linq;
using DomainModels.DataModel;

namespace ACMC_Library_System.DbModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("patron")]
    public partial class patron
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public patron()
        {
            issue = new HashSet<issue>();
            patron1 = new HashSet<patron>();
        }

        public int id { get; set; }

        [StringLength(30)]
        public string barcode { get; set; }

        [StringLength(100)]
        public string surname_ch { get; set; }

        [StringLength(100)]
        public string firstnames_ch { get; set; }

        [StringLength(100)]
        public string surname_en { get; set; }

        [StringLength(100)]
        public string firstnames_en { get; set; }

        [StringLength(30)]
        public string surname_pinyin { get; set; }

        [StringLength(30)]
        public string firstnames_pinyin { get; set; }

        [Column(TypeName = "image")]
        public byte[] picture { get; set; }

        public int? limit { get; set; }

        public int? items_out { get; set; }

        [StringLength(255)]
        public string address { get; set; }

        [StringLength(30)]
        public string phone { get; set; }

        [StringLength(255)]
        public string email { get; set; }

        public int? guarantor { get; set; }

        public DateTime? created { get; set; }

        public DateTime? expiry { get; set; }

        public int? fellowship { get; set; }

        [Column(TypeName = "money")]
        public decimal? balance { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<issue> issue { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<patron> patron1 { get; set; }

        public virtual patron patron2 { get; set; }

        #region Extend Properties

        //following properties will not be in the database
        [NotMapped]
        public bool AllowToDelete => id != BusinessRules.MainUserId && barcode != BusinessRules.MainUserBarcode;

        [NotMapped]
        public string DisplayNameCh => surname_ch == null ? firstnames_ch : $"{surname_ch} {firstnames_ch}";

        [NotMapped]
        public string DisplayNameEn => $"{firstnames_en} {surname_en}";

        [NotMapped]
        public bool HasPhoto => picture != null && picture.Length > 0;

        [NotMapped]
        public string DisplayNameTitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(firstnames_ch + surname_ch))
                {
                    return surname_ch == null ? firstnames_ch : $"{surname_ch} {firstnames_ch}";
                }
                if (!string.IsNullOrWhiteSpace(firstnames_en + surname_en))
                {
                    return $"{firstnames_en} {surname_en}";
                }
                return string.Empty;
            }
        }

        [NotMapped]
        public List<item> BorrowingItems { get; set; }

        /// <summary>
        /// When user is holding a over due item, do not allow him/her to borrow more
        /// </summary>
        [NotMapped]
        public bool CanBorrowItem
        {
            get
            {
                return expiry > DateTime.Today &&
                       (BorrowingItems == null || BorrowingItems.All(item => item.due_date > DateTime.Today) && limit > BorrowingItems.Count);
            }
        }

        #endregion
    }
}

namespace ACMC_Library_System.DbModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class action_type
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public action_type()
        {
            action_history = new HashSet<action_history>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }

        [Required]
        [StringLength(30)]
        public string verb { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<action_history> action_history { get; set; }

        #region Extend property

        public enum ActionTypeEnum
        {
            Order = 1,
            Sale = 2,
            Reserve = 3,
            Lend = 4,
            Renew = 5,
            Return = 6,
            Lost = 7,
            Fine = 8
        }

        #endregion
    }
}

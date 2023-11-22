using System.ComponentModel.DataAnnotations.Schema;

namespace DistributedLocks.Domain
{
    public class EntityIdToLockId
    {
        public Guid EntityId { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LockId { get; set; }
    }
}

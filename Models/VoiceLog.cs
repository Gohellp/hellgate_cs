using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hellgate.Models
{
    public class VoiceLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(25)]
        public required string VoiceName { get; set; }

        [Required]
        [StringLength(10)]
        public required string Event { get; set;}

        [Required]
        [StringLength(25)]
        public required string EventEmitterId { get; set;}
        /*
         * create
         * connect
         * leave
         * streamStart
         * streamEnd
         * muted
         * defened
         * serverMuted
         * serverDefened
         * --mb--
         * activityStart
         * activityEnd
         * ativityConnect
         */

        [Required]
        public required string Description { get; set;}

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime EventHandled { get; set;}
    }
}
